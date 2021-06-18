// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BetterAPI.Events;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace BetterAPI.TestServer
{
    internal sealed class FileSnapshotStore : ISnapshotStore
    {
        public FileSnapshotStore()
        {
            Directory.CreateDirectory("snapshots");
        }

        public async Task SaveRequestAsync(HttpContext context, string url, string body)
        {
            var cacheKey = Base64UrlEncoder.Encode(url);

            var sb = Pooling.StringBuilderPool.Get();
            try
            {
                sb.Append(context.Request.Method);
                sb.Append(' ');
                sb.Append(url);
                sb.Append(' ');
                sb.AppendLine(context.Request.Protocol);

                if (context.Request.Headers.TryGetHeaderString(out var headers))
                    sb.AppendLine(headers);
            
                if (!string.IsNullOrWhiteSpace(body))
                    sb.AppendLine(body);

                var snapshot = sb.ToString();
                await File.WriteAllTextAsync(Path.Combine("snapshots", $"{cacheKey}.request.snapshot"), snapshot, context.RequestAborted);
            }
            finally
            {
                Pooling.StringBuilderPool.Return(sb);
            }
        }

        public async Task SaveResponseAsync(HttpContext context, string url, string body)
        {
            var cacheKey = Base64UrlEncoder.Encode(url);

            var sb = Pooling.StringBuilderPool.Get();
            try
            {
                sb.Append(context.Request.Method);
                sb.Append(' ');
                sb.Append(url);
                sb.Append(' ');
                sb.AppendLine(context.Request.Protocol);

                if (context.Response.Headers.TryGetHeaderString(out var headers))
                    sb.AppendLine(headers);
            
                if (!string.IsNullOrWhiteSpace(body))
                    sb.AppendLine(body);

                var snapshot = sb.ToString();
                await File.WriteAllTextAsync(Path.Combine("snapshots", $"{cacheKey}.response.snapshot"), snapshot, context.RequestAborted);
            }
            finally
            {
                Pooling.StringBuilderPool.Return(sb);
            }
        }

        public Task<IEnumerable<SnapshotInfo>> GetAsync(CancellationToken cancellationToken)
        {
            var results = new List<SnapshotInfo>();

            var source = Directory.EnumerateFiles("snapshots", "*.snapshot", SearchOption.AllDirectories);
            var snapshots = source.Select(x => x.Replace(".request", string.Empty).Replace(".response", string.Empty)).Distinct();

            foreach (var snapshot in snapshots)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var id = Path.GetFileNameWithoutExtension(snapshot);

                var info = new SnapshotInfo
                {
                    Id = id
                };

                results.Add(info);
            }

            return Task.FromResult((IEnumerable<SnapshotInfo>) results);
        }
    }
}