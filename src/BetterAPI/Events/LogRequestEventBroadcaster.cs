// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Threading.Tasks;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace BetterAPI.Events
{
    /// <summary>
    /// Logs incoming requests trapped by an interceptor, if available.
    /// </summary>
    public sealed class LogRequestEventBroadcaster : IRequestEventBroadcaster
    {
        private readonly IStringLocalizer<LogRequestEventBroadcaster> _localizer;

        public LogRequestEventBroadcaster(IStringLocalizer<LogRequestEventBroadcaster> localizer)
        {
            _localizer = localizer;
        }

        public async Task<bool> OnRequestAsync(HttpContext context, ILogger logger)
        {
            // required to be able to rewind the request for deferred execution
            context.Request.EnableBuffering(); 

            using var sr = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await sr.ReadToEndAsync();

            var url = context.Request.GetDisplayUrl();
            var prefix = _localizer.GetString("REQUEST");

            if (string.IsNullOrWhiteSpace(body))
            {
                if (TryGetHeaderString(context.Request.Headers, out var headers))
                {
                    logger.LogInformation($"{prefix}: {context.Request.Method} {url} {context.Request.Protocol}" + Environment.NewLine + headers);
                }
                else
                {
                    logger.LogInformation($"{prefix}: {context.Request.Method} {url} {context.Request.Protocol}");
                }
            }
            else
            {
                if (TryGetHeaderString(context.Request.Headers, out var headers))
                {
                    logger.LogInformation($"{prefix}: {context.Request.Method} {url} {context.Request.Protocol}" + Environment.NewLine + headers + Environment.NewLine + body);
                }
                else
                {
                    logger.LogInformation($"{prefix}: {context.Request.Method} {url} {context.Request.Protocol}" + Environment.NewLine + body);
                }
            }

            // we are deferring here
            return true;
        }

        public async Task OnResponseAsync(HttpContext context, ILogger logger)
        {
            using var sr = new StreamReader(context.Response.Body, leaveOpen: true);
            var body = await sr.ReadToEndAsync();

            var url = context.Request.GetDisplayUrl();
            var prefix = _localizer.GetString("RESPONSE");

            if (string.IsNullOrWhiteSpace(body))
            {
                if (TryGetHeaderString(context.Response.Headers, out var headers))
                {
                    logger.LogInformation($"{prefix}: {context.Request.Method} {url} {context.Request.Protocol}" + Environment.NewLine + headers);
                }
                else
                {
                    logger.LogInformation($"{prefix}: {context.Request.Method} {url} {context.Request.Protocol}");
                }
            }
            else
            {
                if (TryGetHeaderString(context.Response.Headers, out var headers))
                {
                    logger.LogInformation($"{prefix}: {context.Request.Method} {url} {context.Request.Protocol}" + Environment.NewLine + headers + Environment.NewLine + body);
                }
                else
                {
                    logger.LogInformation($"{prefix}: {context.Request.Method} {url} {context.Request.Protocol}" + Environment.NewLine + body);
                }
            }
        }

        private static bool TryGetHeaderString(IHeaderDictionary headers, out string? headersString)
        {
            var sb = Pooling.StringBuilderPool.Get();
            try
            {
                var count = 0;
                foreach (var (key, value) in headers)
                {
                    if (key.StartsWith(":"))
                        continue; // ignore HTTP/2 stream pseudo-headers

                    sb.Append(key);
                    sb.Append(':');
                    sb.Append(' ');
                    sb.Append(value);
                    count++;
                    if(count < headers.Count)
                        sb.AppendLine();
                }

                if (sb.Length == 0)
                {
                    headersString = default;
                    return false;
                }

                headersString = sb.ToString();
                return true;
            }
            finally
            {
                Pooling.StringBuilderPool.Return(sb);
            }
        }
    }
}