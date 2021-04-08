// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Threading;

namespace BetterAPI.Metrics.Internal
{
    /// <summary>
    ///     Provides support for volatile operations around a <see cref="double" /> value
    /// </summary>
    internal struct VolatileDouble
    {
        private double _value;

        public static VolatileDouble operator +(VolatileDouble left, VolatileDouble right)
        {
            return Add(left, right);
        }

        private static VolatileDouble Add(VolatileDouble left, VolatileDouble right)
        {
            left.Set(left.Get() + right.Get());
            return left.Get();
        }

        public static VolatileDouble operator -(VolatileDouble left, VolatileDouble right)
        {
            left.Set(left.Get() - right.Get());
            return left.Get();
        }

        public static VolatileDouble operator *(VolatileDouble left, VolatileDouble right)
        {
            left.Set(left.Get() * right.Get());
            return left.Get();
        }

        public static VolatileDouble operator /(VolatileDouble left, VolatileDouble right)
        {
            left.Set(left.Get() / right.Get());
            return left.Get();
        }

        private VolatileDouble(double value) : this()
        {
            Set(value);
        }

        public void Set(double value)
        {
            Thread.VolatileWrite(ref _value, value);
        }

        public double Get()
        {
            return Thread.VolatileRead(ref _value);
        }

        public static implicit operator VolatileDouble(double value)
        {
            return new VolatileDouble(value);
        }

        public static implicit operator double(VolatileDouble value)
        {
            return value.Get();
        }

        public override string ToString()
        {
            return Get().ToString();
        }
    }
}