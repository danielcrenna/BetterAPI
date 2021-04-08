// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Security.Cryptography;

namespace BetterAPI.Metrics.Internal
{
    /// <summary>
    ///     Provides statistically relevant random number generation
    /// </summary>
    internal class Random
    {
        private static readonly RandomNumberGenerator Inner;

        static Random()
        {
            Inner = RandomNumberGenerator.Create();
        }

        public static long NextLong()
        {
            var buffer = new byte[sizeof(long)];
            Inner.GetBytes(buffer);
            var value = BitConverter.ToInt64(buffer, 0);
            return value;
        }
    }
}