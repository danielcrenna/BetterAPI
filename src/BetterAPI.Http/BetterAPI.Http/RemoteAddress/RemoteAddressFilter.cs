// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Net;
using System.Reflection;
using BetterAPI.Caching;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using WyHash;

namespace BetterAPI.Http.RemoteAddress
{
    public sealed class RemoteAddressFilter : ActionFilterAttribute
    {
        private readonly ICacheRegion<HttpContext> _cache;

        public RemoteAddressFilter(ICacheRegion<HttpContext> cache)
        {
            _cache = cache;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var seed = _cache.GetSeed();

            var privateAddress = typeof(IPAddress)
                .GetProperty("PrivateAddress", BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(context.HttpContext.Connection.RemoteIpAddress);

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

                    var hash = BitConverter.GetBytes(WyHash64.ComputeHash64(new ReadOnlySpan<byte>(addressBytes, 4),
                        seed));
                    context.ActionArguments["addressHash"] = hash;
                }
            }
            else
            {
                privateAddress = typeof(IPAddress).GetField("_numbers", BindingFlags.Instance | BindingFlags.NonPublic)?
                    .GetValue(context.HttpContext.Connection.RemoteIpAddress);

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

                        var hash = BitConverter.GetBytes(
                            WyHash64.ComputeHash64(new ReadOnlySpan<byte>(addressBytes, 16), seed));
                        context.ActionArguments["addressHash"] = hash;
                    }
            }

            base.OnActionExecuting(context);
        }
    }
}