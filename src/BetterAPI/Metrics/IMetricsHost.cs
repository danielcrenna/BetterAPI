// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace BetterAPI.Metrics
{
    public interface IMetricsHost : IReadableMetrics
    {
        bool Clear();
        bool TryGetMetric(MetricName name, out IMetric? metric);

        #region Default Owner

        GaugeMetric<T> Gauge<T>(string name, Func<T> evaluator);
        CounterMetric Counter(string name);
        HistogramMetric Histogram(string name, SampleType sampleType);
        MeterMetric Meter(string name, string eventType, TimeUnit rateUnit);
        TimerMetric Timer(string name, TimeUnit durationUnit, TimeUnit rateUnit);

        #endregion

        #region Owner

        GaugeMetric<T> Gauge<T>(Type owner, string name, Func<T> evaluator);
        CounterMetric Counter(Type owner, string name);
        HistogramMetric Histogram(Type owner, string name, SampleType sampleType);
        MeterMetric Meter(Type owner, string name, string eventType, TimeUnit rateUnit);
        TimerMetric Timer(Type owner, string name, TimeUnit durationUnit, TimeUnit rateUnit);

        #endregion
    }
}