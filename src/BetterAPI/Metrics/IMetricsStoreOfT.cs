// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Immutable;

namespace BetterAPI.Metrics
{
    public interface IMetricsStore<TFilter> : ICanClear where TFilter : IMetric
    {
        IMetric this[MetricName key] { get; }
        IImmutableDictionary<MetricName, TFilter> GetSample(MetricType typeFilter = MetricType.None);
        IMetric GetOrAdd(MetricName key, IMetric value);
        bool TryGetValue(MetricName key, out IMetric value);
        bool Contains(MetricName key);
        void AddOrUpdate<T>(MetricName key, T value) where T : IMetric;
    }
}