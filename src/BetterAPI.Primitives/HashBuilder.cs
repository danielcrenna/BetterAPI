// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text;
using WyHash;

namespace BetterAPI
{
    /// <summary>
    /// Provides centralized access to preferred hash implementations.
    /// </summary>
    public static class HashBuilder
    {
        public static class Dictionary
        {
            public static readonly ulong Seed = BitConverter.ToUInt64(Encoding.UTF8.GetBytes(nameof(Dictionary)), 0);

            /// <summary>
            /// Create a hash key for a dictionary.
            ///     <remarks>
            ///         WyHash was chosen due to speed and performance with small keys (i.e. for use in a hash table)
            ///     </remarks>
            ///     <seealso href="https://github.com/rurban/smhasher/" />
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public static ulong Key(ReadOnlySpan<byte> value)
            {
                return WyHash64.ComputeHash64(value, Seed);
            }
        }
    }
}