// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace BetterAPI.Caching
{
    internal static class ETagGenerator
    {
        public static ETag Generate(ReadOnlySpan<byte> buffer)
        {
            using var md5 = MD5.Create();
            var hash = new byte[md5.HashSize / 8];
            var hashed = md5.TryComputeHash(buffer, hash, out _);
            Debug.Assert(hashed);
            var hex = BitConverter.ToString(hash);
            return new ETag(ETagType.Weak, $"W/\"{hex.Replace("-", "")}\"");
        }
    }
}