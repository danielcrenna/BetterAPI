// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;

namespace BetterAPI.Http.Throttling
{
    /// <summary>
    ///     Throttles traffic generically for heavy anonymous operations exposed to public networks.
    ///     <remarks>
    ///         - ASP.NET Core is already exposing the IP address in process memory, so we can't do much about that.
    ///         - We need to hash the IP address to avoid ever storing it in memory outside of what ASP.NET Core is doing.
    ///         - Using the IP address in any other way is a privacy breach.
    ///         - This most likely should be disclosed in any auto-generated privacy policies.         
    ///     </remarks>
    ///     <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Retry-After" />
    /// </summary>
    internal sealed class ThrottleFilter : IAsyncActionFilter
    {
        private static readonly PropertyInfo? PrivateAddressProperty;
        private static readonly FieldInfo? NumbersField;
        
        static ThrottleFilter()
        {
            PrivateAddressProperty = typeof(IPAddress).GetProperty("PrivateAddress", BindingFlags.Instance | BindingFlags.NonPublic);
            NumbersField = typeof(IPAddress).GetField("_numbers", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private readonly Func<DateTimeOffset> _timestamps;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _retryAfterDuration;

        public ThrottleFilter(Func<DateTimeOffset> timestamps, IMemoryCache cache)
        {
            _retryAfterDuration = TimeSpan.FromSeconds(5);
            _timestamps = timestamps;
            _cache = cache;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var hash = GetIpAddressHash(context);
            var cacheKey = HashBuilder.Dictionary.Key(hash);

            var now = _timestamps();
            var retryAfter = now.Add(_retryAfterDuration);
            if (_cache.Get<int>(cacheKey) == 0)
            {
                _cache.Set(cacheKey, 1, retryAfter);
                await next();
            }
            else
            {
                TooManyRequests(context, retryAfter);
            }
        }

        private static byte[] GetIpAddressHash(ActionContext context)
        {
            var privateAddress = PrivateAddressProperty?.GetValue(context.HttpContext.Connection.RemoteIpAddress);

            if (privateAddress != default)
            {
                unsafe
                {
                    var address = (uint) privateAddress;
                    var addressBytes = stackalloc byte[4];
                    addressBytes[0] = (byte) address;
                    addressBytes[1] = (byte) (address >> 8);
                    addressBytes[2] = (byte) (address >> 16);
                    addressBytes[3] = (byte) (address >> 24);
                    var hash = BitConverter.GetBytes(HashBuilder.Dictionary.Key(new ReadOnlySpan<byte>(addressBytes, 4)));
                    return hash;
                }
            }

            privateAddress = NumbersField?.GetValue(context.HttpContext.Connection.RemoteIpAddress);

            if (privateAddress != default)
                unsafe
                {
                    var address = (ushort[]) privateAddress;
                    var addressBytes = stackalloc byte[16];
                    var j = 0;
                    for (var i = 0; i < 8; i++)
                    {
                        addressBytes[j++] = (byte) ((address[i] >> 8) & 0xFF);
                        addressBytes[j++] = (byte) (address[i] & 0xFF);
                    }
                    var hash = BitConverter.GetBytes(HashBuilder.Dictionary.Key(new ReadOnlySpan<byte>(addressBytes, 16)));
                    return hash;
                }

            return Array.Empty<byte>();
        }

        private void TooManyRequests(ActionContext context, DateTimeOffset retryAfter)
        {
            context.HttpContext.Response.StatusCode = (int) HttpStatusCode.TooManyRequests;
            context.HttpContext.Response.Headers.TryAdd(HeaderNames.RetryAfter, retryAfter.ToString("r"));
            context.HttpContext.Response.Headers.TryAdd(HeaderNames.RetryAfter, _retryAfterDuration.Seconds.ToString());
        }
    }
}