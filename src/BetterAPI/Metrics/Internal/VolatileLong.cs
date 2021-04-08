// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Threading;

namespace BetterAPI.Metrics.Internal
{
    /// <summary>
    ///     Provides support for volatile operations around a <see cref="long" /> value
    /// </summary>
    internal struct VolatileLong
    {
        private long _value;

        public static VolatileLong operator +(VolatileLong left, VolatileLong right)
        {
            return Add(left, right);
        }

        private static VolatileLong Add(VolatileLong left, VolatileLong right)
        {
            left.Set(left.Get() + right.Get());
            return left.Get();
        }

        public static VolatileLong operator -(VolatileLong left, VolatileLong right)
        {
            left.Set(left.Get() - right.Get());
            return left.Get();
        }

        public static VolatileLong operator *(VolatileLong left, VolatileLong right)
        {
            left.Set(left.Get() * right.Get());
            return left.Get();
        }

        public static VolatileLong operator /(VolatileLong left, VolatileLong right)
        {
            left.Set(left.Get() / right.Get());
            return left.Get();
        }

        private VolatileLong(VolatileLong value) : this()
        {
            Set(value);
        }

        public void Set(long value)
        {
            Thread.VolatileWrite(ref _value, value);
        }

        public long Get()
        {
            return Thread.VolatileRead(ref _value);
        }

        public static implicit operator VolatileLong(long value)
        {
            var result = new VolatileLong();
            result.Set(value);
            return result;
        }

        public static implicit operator long(VolatileLong value)
        {
            return value.Get();
        }

        public override string ToString()
        {
            return Get().ToString();
        }
    }
}