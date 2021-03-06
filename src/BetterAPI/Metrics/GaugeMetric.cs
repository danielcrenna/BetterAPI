// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Runtime.Serialization;
using BetterAPI.Extensions;

namespace BetterAPI.Metrics
{
    public abstract class GaugeMetric : IMetric
    {
        public abstract string ValueAsString { get; }
        public abstract bool IsNumeric { get; }
        public abstract bool IsBoolean { get; }

        [IgnoreDataMember] public abstract MetricName Name { get; }

        public int CompareTo(IMetric other)
        {
            return other.Name.CompareTo(Name);
        }

        public abstract IMetric Copy();
    }

    /// <summary>
    ///     A gauge metric is an instantaneous reading of a particular value. To
    ///     instrument a queue's depth, for example:
    ///     <example>
    ///         <code> 
    /// var queue = new Queue{int}();
    /// var gauge = new GaugeMetric{int}(() => queue.Count);
    /// </code>
    ///     </example>
    /// </summary>
    public class GaugeMetric<T> : GaugeMetric
    {
        private readonly Func<T> _evaluator;

        internal GaugeMetric(MetricName metricName, Func<T> evaluator)
        {
            Name = metricName;
            _evaluator = evaluator;
            IsNumeric = typeof(T).IsNumeric();
            IsBoolean = typeof(T).IsTruthy();
        }

        [IgnoreDataMember] public override MetricName Name { get; }

        public T Value => _evaluator();

        public override string ValueAsString => Value?.ToString();

        public override bool IsNumeric { get; }

        public override bool IsBoolean { get; }

        public override IMetric Copy()
        {
            return new GaugeMetric<T>(Name, _evaluator);
        }
    }
}