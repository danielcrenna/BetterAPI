// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.IO;
using System.Threading.Tasks;
using BetterAPI.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

namespace BetterAPI.TestServer
{
    // FEATURE: smarter cache key (allow for parametrization for matching)
    // FEATURE: need a toggle to enable/disable collector mode
    // FEATURE: add multiple snapshots per request/response 
    // FEATURE: expiration/age-out of older requests
    // FEATURE: change format?
    // FEATURE: store in LMDB?

    /// <summary>
    /// When running as a test collector, save request/response pairs as snapshots.
    /// </summary>
    public sealed class SnapshotRequestEventBroadcaster : IRequestEventBroadcaster
    {
        private readonly ISnapshotStore _store;

        public SnapshotRequestEventBroadcaster(ISnapshotStore store)
        {
            _store = store;
        }

        public async Task<bool> OnRequestAsync(HttpContext context, ILogger logger)
        {
            // required to be able to rewind the request for deferred execution
            context.Request.EnableBuffering(); 

            using var sr = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await sr.ReadToEndAsync();
            var url = context.Request.GetDisplayUrl();

            await _store.SaveRequestAsync(context, url, body);

            // we are deferring here
            return true;
        }

        public async Task OnResponseAsync(HttpContext context, ILogger logger)
        {
            using var sr = new StreamReader(context.Response.Body, leaveOpen: true);
            var body = await sr.ReadToEndAsync();
            var url = context.Request.GetDisplayUrl();

            await _store.SaveResponseAsync(context, url, body);
        }
    }
}