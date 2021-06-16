// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BetterAPI.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IO;

namespace BetterAPI.Http.Interception
{
    internal sealed class RequestInterceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RecyclableMemoryStreamManager _manager;
        private readonly ILogger _logger;

        public RequestInterceptionMiddleware(RequestDelegate next, RecyclableMemoryStreamManager manager, ILoggerFactory loggerFactory)
        {
            _next = next;
            _manager = manager;
            _logger = loggerFactory.CreateLogger<RequestInterceptionMiddleware>();
        }

        // ReSharper disable once UnusedMember.Global: Called via reflection
        public async Task Invoke(HttpContext context)
        {
            var options = context.RequestServices.GetRequiredService<IOptionsSnapshot<RequestInterceptionOptions>>();

            if (!options.Value.Enabled)
            {
                await _next(context);
                return; // not enabled
            }

            var broadcasters = context.RequestServices.GetServices<IRequestEventBroadcaster>().AsList();
            if (broadcasters?.Count == 0)
            {
                await _next(context);
                return; // nothing to respond to
            }

            var defer = await WithRequest(context, broadcasters!);
            if(!defer)
            {
                await _next(context); // hand off without wrapping the response
            }
            else
            {
                // if we deferred, we also need to seek the request for the benefit of downstream middleware
                if(context.Request.Body.CanSeek)
                    context.Request.Body.Seek(0, SeekOrigin.Begin);     
            }

            await WithResponse(context, defer, broadcasters!);
        }
  
        private async Task<bool> WithRequest(HttpContext context, IEnumerable<IRequestEventBroadcaster> broadcasters)
        {
            var defer = false;
            foreach (var broadcaster in broadcasters)
            {
                defer |= await broadcaster.OnRequestAsync(context, _logger);
            }
            return defer;
        }

        private async Task WithResponse(HttpContext context, bool defer, IEnumerable<IRequestEventBroadcaster> broadcasters)
        {
            if (defer)
            {
                var wrapped = context.Response.Body;

                await using var body = _manager.GetStream();
                context.Response.Body = body;
                await _next(context); // write to the seekable stream

                context.Response.Body.Seek(0, SeekOrigin.Begin); // first rewind for broadcasters
                foreach (var broadcaster in broadcasters)
                    await broadcaster.OnResponseAsync(context, _logger);
                
                context.Response.Body.Seek(0, SeekOrigin.Begin); // second rewind to copy to the waiting HTTP stream
                await context.Response.Body.CopyToAsync(wrapped);
                context.Response.Body = wrapped;
            }
            else
            {
                foreach (var broadcaster in broadcasters)
                {
                    await broadcaster.OnResponseAsync(context, _logger);
                }
            }
        }
    }
}