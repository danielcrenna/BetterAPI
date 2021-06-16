// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.IO;
using System.Threading.Tasks;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace BetterAPI.Events
{
    // FEATURE: smarter cache key (allow for parametrization for matching)
    // FEATURE: need a toggle to enable/disable collector mode
    // FEATURE: add multiple snapshots per request/response 
    // FEATURE: change format?
    // FEATURE: store in LMDB?

    /// <summary>
    /// When running as a test collector, save request/response pairs as snapshots.
    /// </summary>
    public sealed class SnapshotRequestEventBroadcaster : IRequestEventBroadcaster
    {
        public SnapshotRequestEventBroadcaster()
        {
            Directory.CreateDirectory("snapshots");
        }

        public async Task<bool> OnRequestAsync(HttpContext context, ILogger logger)
        {
            // required to be able to rewind the request for deferred execution
            context.Request.EnableBuffering(); 

            using var sr = new StreamReader(context.Request.Body, leaveOpen: true);

            var body = await sr.ReadToEndAsync();
            var url = context.Request.GetDisplayUrl();
            var cacheKey = Base64UrlEncoder.Encode(url);

            var sb = Pooling.StringBuilderPool.Get();
            try
            {
                sb.AppendLine(context.Request.Method);
                sb.AppendLine(url);
                sb.AppendLine(context.Request.Protocol);

                if (context.Request.Headers.TryGetHeaderString(out var headers))
                    sb.AppendLine(headers);
            
                if (!string.IsNullOrWhiteSpace(body))
                    sb.AppendLine(body);

                var snapshot = sb.ToString();
                await File.WriteAllTextAsync(Path.Combine("snapshots", $"{cacheKey}.request.snapshot"), snapshot);
            }
            finally
            {
                Pooling.StringBuilderPool.Return(sb);
            }

            // we are deferring here
            return true;
        }

        public async Task OnResponseAsync(HttpContext context, ILogger logger)
        {
            using var sr = new StreamReader(context.Response.Body, leaveOpen: true);

            var body = await sr.ReadToEndAsync();
            var url = context.Request.GetDisplayUrl();
            var cacheKey = Base64UrlEncoder.Encode(url);

            var sb = Pooling.StringBuilderPool.Get();
            try
            {
                sb.AppendLine(context.Request.Method);
                sb.AppendLine(url);
                sb.AppendLine(context.Request.Protocol);

                if (context.Response.Headers.TryGetHeaderString(out var headers))
                    sb.AppendLine(headers);
            
                if (!string.IsNullOrWhiteSpace(body))
                    sb.AppendLine(body);

                var snapshot = sb.ToString();
                await File.WriteAllTextAsync(Path.Combine("snapshots", $"{cacheKey}.response.snapshot"), snapshot);
            }
            finally
            {
                Pooling.StringBuilderPool.Return(sb);
            }
        }
    }
}