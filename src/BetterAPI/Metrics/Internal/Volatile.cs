// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Threading;

namespace BetterAPI.Metrics.Internal
{
    /// <summary>
    ///     Provides support for volatile operations around a typed value
    /// </summary>
    internal struct Volatile<T>
    {
        private object _value;

        private Volatile(T value) : this()
        {
            Set(value);
        }

        public void Set(T value)
        {
            Thread.VolatileWrite(ref _value, value);
        }

        public T Get()
        {
            return (T) Thread.VolatileRead(ref _value);
        }

        public static implicit operator Volatile<T>(T value)
        {
            return new Volatile<T>(value);
        }

        public static implicit operator T(Volatile<T> value)
        {
            return value.Get();
        }

        public override string ToString()
        {
            return Get().ToString();
        }
    }
}