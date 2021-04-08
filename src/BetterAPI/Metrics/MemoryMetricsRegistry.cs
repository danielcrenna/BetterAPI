// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace BetterAPI.Metrics
{
    public class MemoryMetricsRegistry : IMetricsRegistry
    {
        private readonly ConcurrentDictionary<string, IMetricsHost> _registry;

        public MemoryMetricsRegistry()
        {
            _registry = new ConcurrentDictionary<string, IMetricsHost>();
        }

        public void Add(IMetricsHost host)
        {
            var key = $"{Environment.MachineName}.{Environment.CurrentManagedThreadId}";
            _registry.AddOrUpdate(key, host, (n, r) => r);
        }

        public bool TryGetMetric(MetricName name, out IMetric? metric)
        {
            foreach (var host in this)
                if (host.TryGetMetric(name, out metric))
                    return true;

            metric = default;
            return false;
        }

        public IEnumerator<IMetricsHost> GetEnumerator()
        {
            return _registry.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}