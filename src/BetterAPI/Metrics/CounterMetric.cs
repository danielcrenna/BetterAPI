// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using BetterAPI.Metrics.Internal;

namespace BetterAPI.Metrics
{
    /// <summary>
    ///     An atomic counter metric
    /// </summary>
    public sealed class CounterMetric : IMetric, IComparable<CounterMetric>, IComparable
    {
        private readonly AtomicLong _count = new AtomicLong(0);

        internal CounterMetric(MetricName metricName)
        {
            Name = metricName;
        }

        private CounterMetric(MetricName metricName, AtomicLong count)
        {
            Name = metricName;
            _count = count;
        }

        public long Count => _count.Get();

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            return obj is CounterMetric other
                ? CompareTo(other)
                : throw new ArgumentException($"Object must be of type {nameof(CounterMetric)}");
        }

        public int CompareTo(CounterMetric other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return Name.CompareTo(other.Name);
        }

        [IgnoreDataMember] public MetricName Name { get; }

        public int CompareTo(IMetric other)
        {
            return other.Name.CompareTo(Name);
        }

        internal IMetric Copy()
        {
            var copy = new CounterMetric(Name, new AtomicLong(_count));
            return copy;
        }

        public void Increment()
        {
            Increment(1);
        }

        public long Increment(long amount)
        {
            return _count.AddAndGet(amount);
        }

        public long Decrement()
        {
            return Decrement(1);
        }

        public long Decrement(long amount)
        {
            return _count.AddAndGet(0 - amount);
        }

        public void Clear()
        {
            _count.Set(0);
        }

        public static bool operator <(CounterMetric left, CounterMetric right)
        {
            return Comparer<CounterMetric>.Default.Compare(left, right) < 0;
        }

        public static bool operator >(CounterMetric left, CounterMetric right)
        {
            return Comparer<CounterMetric>.Default.Compare(left, right) > 0;
        }

        public static bool operator <=(CounterMetric left, CounterMetric right)
        {
            return Comparer<CounterMetric>.Default.Compare(left, right) <= 0;
        }

        public static bool operator >=(CounterMetric left, CounterMetric right)
        {
            return Comparer<CounterMetric>.Default.Compare(left, right) >= 0;
        }
    }
}