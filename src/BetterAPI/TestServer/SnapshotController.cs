// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using BetterAPI.Http;
using Microsoft.AspNetCore.Mvc;

namespace BetterAPI.TestServer
{
    [InternalController]
    [Display(Name = "Snapshots", Description = "Provides a directory of request snapshots for test server operation.")]
    public sealed class SnapshotController : Controller
    {
        private readonly ISnapshotStore _store;

        public SnapshotController(ISnapshotStore store)
        {
            _store = store;
        }

        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var snapshots = await _store.GetAsync(cancellationToken);

            return Ok(new
            {
                Value = snapshots
            });
        }
    }
}