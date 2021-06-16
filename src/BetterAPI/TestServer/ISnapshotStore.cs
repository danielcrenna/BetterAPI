// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BetterAPI.TestServer
{
    public interface ISnapshotStore
    {
        Task SaveRequestAsync(HttpContext context, string url, string body);
        Task SaveResponseAsync(HttpContext context, string url, string body);

        Task<IEnumerable<SnapshotInfo>> GetAsync(CancellationToken cancellationToken);
    }
}