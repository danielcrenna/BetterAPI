// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BetterAPI.Metrics
{
    public class MetricsBuilder : IMetricsBuilder
    {
        private readonly IHealthChecksBuilder _builder;
        private readonly IList<Action<IMetricsHost>> _eventSourceCounters;
        private readonly HashSet<string> _eventSourceNames;

        public MetricsBuilder(IServiceCollection services, IHealthChecksBuilder builder)
        {
            Services = services;
            _builder = builder;
            _eventSourceCounters = new List<Action<IMetricsHost>>();
            _eventSourceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public IServiceCollection Services { get; }

        public ISet<string> Build(IMetricsHost host)
        {
            foreach (var registration in _eventSourceCounters)
                registration(host);

            return _eventSourceNames;
        }

        public MetricsBuilder RegisterEventSourceCounter(string eventSource, string name)
        {
            // Normalize on inconsistent naming choices: i.e. "Microsoft-AspNetCore-Server-Kestrel"
            eventSource = eventSource.Replace("-", ".");
            _eventSourceCounters.Add(host => host.Counter($"{eventSource}.{name}"));
            _eventSourceNames.Add(eventSource);
            return this;
        }

        public MetricsBuilder RegisterEventSourceCounterAsHealthCheck(string eventSource, string name,
            Func<long, bool> checkFunc)
        {
            RegisterAsHealthCheck(host => host.Counter($"{eventSource}.{name}"), checkFunc);
            return this;
        }

        public MetricsBuilder RegisterAsHealthCheck(Func<IMetricsHost, GaugeMetric<bool>> builderFunc,
            HealthStatus onCheckFailure = HealthStatus.Unhealthy)
        {
            return RegisterAsHealthCheck(builderFunc, m => m, onCheckFailure);
        }

        public MetricsBuilder RegisterAsHealthCheck<T>(Func<IMetricsHost, GaugeMetric<T>> builderFunc,
            Func<T, bool> checkFunc, HealthStatus onCheckFailure = HealthStatus.Unhealthy)
        {
            return RegisterAsHealthCheck(builderFunc, m => m.Value, checkFunc, onCheckFailure);
        }

        public MetricsBuilder RegisterAsHealthCheck(Func<IMetricsHost, CounterMetric> builderFunc,
            Func<long, bool> checkFunc, HealthStatus onCheckFailure = HealthStatus.Unhealthy)
        {
            return RegisterAsHealthCheck(builderFunc, m => m.Count, checkFunc, onCheckFailure);
        }

        public MetricsBuilder RegisterAsHealthCheck<TMetric, TValue>(Func<IMetricsHost, TMetric> builderFunc,
            Func<TMetric, TValue> valueFunc, Func<TValue, bool> checkFunc,
            HealthStatus onCheckFailure = HealthStatus.Unhealthy) where TMetric : IMetric
        {
            // FIXME: throwaway host to resolve the name, which we need in advance for registration
            var name = builderFunc(new MetricsHost(new MemoryMetricsStore())).Name.Name;

            Services.AddSingleton(r => new MetricHealthCheck<TMetric, TValue>(name,
                r.GetRequiredService<IMetricsHost>(), builderFunc, valueFunc, checkFunc, onCheckFailure));
            _builder.Add(new HealthCheckRegistration(name,
                r => r.GetRequiredService<MetricHealthCheck<TMetric, TValue>>(), onCheckFailure, null, null));
            _builder.AddCheck<MetricHealthCheck<TMetric, TValue>>($"health_check.{name}");
            return this;
        }
    }
}