// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using WyHash;

namespace BetterAPI.Data
{
    internal static class LightningKeyBuilder
    {
        public static ReadOnlySpan<byte> Key(ReadOnlySpan<char> value)
        {
            var buffer = new char[value.Length];
            var result = new byte[sizeof(ulong)];

            value.ToUpperInvariant(buffer);

            if(!BitConverter.TryWriteBytes(result, WyHash64.ComputeHash64(buffer)))
                throw new InvalidOperationException();

            return result;
        }
    }
}