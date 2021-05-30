// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BetterAPI.Metrics.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BetterAPI.Metrics
{
    [Route("api/metrics")]
    public class MetricsController : Controller
    {
        private readonly IOptionsSnapshot<MetricsOptions> _options;
        private readonly IMetricsRegistry _registry;

        public MetricsController(IMetricsRegistry registry, IOptionsSnapshot<MetricsOptions> options)
        {
            _registry = registry;
            _options = options;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            var timeout = TimeSpan.FromSeconds(_options.Value.SampleTimeoutSeconds);
            var cancel = new CancellationTokenSource(timeout);
            var samples = await Task.Run(() => _registry.SelectMany(x => x.GetSample()).ToImmutableDictionary(),
                cancel.Token);
            var json = JsonSampleSerializer.Serialize(samples);
            return Ok(json);
        }
    }
}