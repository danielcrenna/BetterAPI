// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace BetterAPI.Metrics
{
    public class MemoryMetricsStore : IMetricsStore
    {
        private static readonly IImmutableDictionary<MetricName, IMetric> NoSample =
            ImmutableDictionary.Create<MetricName, IMetric>();

        private readonly ConcurrentDictionary<MetricName, IMetric> _inner;

        public MemoryMetricsStore()
        {
            _inner = new ConcurrentDictionary<MetricName, IMetric>();
        }

        public IImmutableDictionary<MetricName, IMetric> GetSample(MetricType typeFilter = MetricType.All)
        {
            if (typeFilter.HasFlagFast(MetricType.All)) return NoSample;

            var filtered = new Dictionary<MetricName, IMetric>();
            foreach (var entry in _inner)
                switch (entry.Value)
                {
                    case GaugeMetric _ when typeFilter.HasFlagFast(MetricType.Gauge):
                    case CounterMetric _ when typeFilter.HasFlagFast(MetricType.Counter):
                    case MeterMetric _ when typeFilter.HasFlagFast(MetricType.Meter):
                    case HistogramMetric _ when typeFilter.HasFlagFast(MetricType.Histogram):
                    case TimerMetric _ when typeFilter.HasFlagFast(MetricType.Timer):
                        continue;

                    default:
                        filtered.Add(entry.Key, entry.Value);
                        break;
                }

            return filtered.ToImmutableSortedDictionary(k => k.Key, v =>
            {
                return v.Value switch
                {
                    GaugeMetric gauge => gauge.Copy(),
                    CounterMetric counter => counter.Copy(),
                    MeterMetric meter => meter.Copy(),
                    HistogramMetric histogram => histogram.Copy(),
                    TimerMetric timer => timer.Copy(),
                    _ => throw new ArgumentException()
                };
            });
        }

        public IMetric GetOrAdd(MetricName key, IMetric value)
        {
            return _inner.GetOrAdd(key, value);
        }

        public bool TryGetValue(MetricName key, out IMetric value)
        {
            return _inner.TryGetValue(key, out value);
        }

        public bool Contains(MetricName key)
        {
            return _inner.ContainsKey(key);
        }

        public void AddOrUpdate<T>(MetricName key, T value) where T : IMetric
        {
            _inner.AddOrUpdate(key, k => value, (k, v) => value);
        }

        public IMetric this[MetricName key] => _inner[key];

        public bool Clear()
        {
            _inner.Clear();
            return true;
        }
    }
}