// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace BetterAPI.Events
{
    /// <summary>
    /// Logs incoming requests trapped by an interceptor, if available.
    /// This requires using `services.AddRequestInterception()` and `app.UseRequestInterception()` to function.
    /// </summary>
    public sealed class LogRequestEventBroadcaster : IRequestEventBroadcaster
    {
        private readonly IStringLocalizer<LogRequestEventBroadcaster> _localizer;
        private readonly LogLevel _level;

        public LogRequestEventBroadcaster(IStringLocalizer<LogRequestEventBroadcaster> localizer, LogLevel level)
        {
            _localizer = localizer;
            _level = level;
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
                if (context.Request.Headers.TryGetHeaderString(out var headers))
                {
                    if(logger.IsEnabled(_level))
                        logger.Log(_level, $"{prefix}: {context.Request.Method} {url} {context.Request.Protocol}" + Environment.NewLine + headers);
                }
                else
                {
                    if(logger.IsEnabled(_level))
                        logger.Log(_level, $"{prefix}: {context.Request.Method} {url} {context.Request.Protocol}");
                }
            }
            else
            {
                if (context.Request.Headers.TryGetHeaderString(out var headers))
                {
                    if(logger.IsEnabled(_level))
                        logger.Log(_level, $"{prefix}: {context.Request.Method} {url} {context.Request.Protocol}" + Environment.NewLine + headers + Environment.NewLine + body);
                }
                else
                {
                    if(logger.IsEnabled(_level))
                        logger.Log(_level, $"{prefix}: {context.Request.Method} {url} {context.Request.Protocol}" + Environment.NewLine + body);
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
                if (context.Response.Headers.TryGetHeaderString(out var headers))
                {
                    if(logger.IsEnabled(_level))
                        logger.Log(_level, $"{prefix}: {context.Request.Method} {url} {context.Request.Protocol}" + Environment.NewLine + headers);
                }
                else
                {
                    if(logger.IsEnabled(_level))
                        logger.Log(_level, $"{prefix}: {context.Request.Method} {url} {context.Request.Protocol}");
                }
            }
            else
            {
                if (context.Response.Headers.TryGetHeaderString(out var headers))
                {
                    if(logger.IsEnabled(_level))
                        logger.Log(_level, $"{prefix}: {context.Request.Method} {url} {context.Request.Protocol}" + Environment.NewLine + headers + Environment.NewLine + body);
                }
                else
                {
                    if(logger.IsEnabled(_level))
                        logger.Log(_level, $"{prefix}: {context.Request.Method} {url} {context.Request.Protocol}" + Environment.NewLine + body);
                }
            }
        }
    }
}