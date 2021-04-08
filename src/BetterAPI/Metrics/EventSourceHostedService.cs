// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BetterAPI.Metrics
{
    /// <summary>
    ///     Acts as a bridge between .NET EventSources and the internal metrics stack.
    /// </summary>
    internal sealed class EventSourceHostedService : EventListener, IHostedService
    {
        private readonly IDisposable _changes;
        private readonly IOptionsMonitor<MetricsOptions> _options;
        private readonly IMetricsRegistry _registry;
        private readonly HashSet<string>? _sourceNames;

        private readonly HashSet<EventSource> _sources;

        private Task? _poll;

        public EventSourceHostedService(IMetricsHost host, IMetricsRegistry registry,
            IEnumerable<IMetricsBuilder> builders, IOptionsMonitor<MetricsOptions> options)
        {
            _sources = new HashSet<EventSource>();
            _sourceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var builder in builders)
            foreach (var source in builder.Build(host))
                _sourceNames.Add(source);

            _registry = registry;
            _options = options;

            _changes = _options.OnChange(o =>
            {
                foreach (var source in _sources)
                {
                    DisableEvents(source);
                    SubscribeToEventSource(source);
                }
            });
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _poll = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    foreach (var eventSource in EventSource.GetSources())
                        OnEventSourceCreated(eventSource);

                    await Task.Delay(1000, cancellationToken);
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _changes.Dispose();
            _poll?.Dispose();
            return Task.CompletedTask;
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (_sourceNames == null)
                return; // in reflection-scenarios (i.e. testing), the listener can fire without object instantiation

            if (!_sourceNames.Contains(eventSource.Name))
                return; // not registered for this event source

            _sources.Add(eventSource);

            // listen to all events on this source with the configured polling interval
            SubscribeToEventSource(eventSource);
        }

        private void SubscribeToEventSource(EventSource eventSource)
        {
            EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All, new Dictionary<string, string?>
            {
                {"EventCounterIntervalSec", _options.CurrentValue.EventSourceIntervalSeconds.ToString()}
            });
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName != "EventCounters" || eventData.Payload == null || eventData.Payload.Count == 0)
                return; // not a counter with readable data

            if (eventData.Payload[0] == null || eventData.Payload[0] is not IDictionary<string, object>)
                return; // expecting a dictionary

            var payload = (IDictionary<string, object>) eventData.Payload[0]!;

            if (!payload.TryGetValue("CounterType", out var counterType) ||
                !payload.TryGetValue("Name", out var name))
                return; // payload is missing counter type and name

            if (!(counterType is string counterTypeString) || string.IsNullOrWhiteSpace(counterTypeString))
                return; // invalid counter type

            if (!(name is string nameString) || string.IsNullOrWhiteSpace(nameString))
                return; // invalid name

            var metricName = $"{eventData.EventSource.Name}.{nameString}";

            if (!_registry.TryGetMetric(new MetricName(typeof(MetricsHost), metricName), out var metric))
                throw new InvalidOperationException(
                    $"EventSource '{eventData.EventSource.Name}' was registered for metrics, but no metric found for '{metricName}'");

            if (!(metric is CounterMetric counter))
                throw new InvalidOperationException(
                    $"EventSource '{eventData.EventSource.Name}' was registered for metrics, but metric '{metricName}' is not a counter.");

            if (counterTypeString.Equals("Sum") && payload.TryGetValue("Increment", out var increment))
            {
                if (increment is double value)
                {
                    if (value == 0d)
                        return;

                    if (value < 0d)
                        counter.Decrement(Convert.ToInt64(value));
                    else
                        counter.Increment(Convert.ToInt64(value));
                }
                else
                {
                    throw new InvalidOperationException("unexpected increment type " + increment.GetType());
                }
            }
        }
    }
}