// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BetterAPI.Metrics
{
    public sealed class MetricHealthCheck<TMetric, TValue> : IHealthCheck
    {
        private readonly Func<IMetricsHost, TMetric> _builderFunc;
        private readonly Func<TValue, bool> _checkFunc;
        private readonly IMetricsHost _host;
        private readonly string _name;
        private readonly HealthStatus _onCheckFailure;
        private readonly Func<TMetric, TValue> _valueFunc;

        public MetricHealthCheck(string name, IMetricsHost host, Func<IMetricsHost, TMetric> builderFunc,
            Func<TMetric, TValue> valueFunc, Func<TValue, bool> checkFunc,
            HealthStatus onCheckFailure = HealthStatus.Unhealthy)
        {
            _name = name;
            _host = host;
            _builderFunc = builderFunc;
            _valueFunc = valueFunc;
            _checkFunc = checkFunc;
            _onCheckFailure = onCheckFailure;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = new CancellationToken())
        {
            TMetric metric;
            try
            {
                metric = _builderFunc(_host);
            }
            catch (Exception e)
            {
                return Task.FromResult(new HealthCheckResult(_onCheckFailure,
                    "Could not create metric for health check", e));
            }

            try
            {
                var value = _valueFunc(metric);
                var result = new HealthCheckResult(_checkFunc(value) ? HealthStatus.Healthy : _onCheckFailure,
                    $"{_name} returned value of {value}");

                return Task.FromResult(result);
            }
            catch (Exception e)
            {
                return Task.FromResult(new HealthCheckResult(_onCheckFailure, e.Message, e));
            }
        }
    }
}