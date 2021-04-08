// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;

namespace BetterAPI.Metrics.Internal
{
    internal static class CollectionExtensions
    {
        public static IDictionary<string, IDictionary<string, IMetric>> Sort(
            this IReadOnlyDictionary<MetricName, IMetric> metrics)
        {
            var sortedMetrics = new SortedDictionary<string, IDictionary<string, IMetric>>();

            foreach (var entry in metrics)
            {
                var className = entry.Key.Class.Name;
                IDictionary<string, IMetric> submetrics;
                if (!sortedMetrics.ContainsKey(className))
                {
                    submetrics = new SortedDictionary<string, IMetric>();
                    sortedMetrics.Add(className, submetrics);
                }
                else
                {
                    submetrics = sortedMetrics[className];
                }

                submetrics.Add(entry.Key.Name, entry.Value);
            }

            return sortedMetrics;
        }
    }
}