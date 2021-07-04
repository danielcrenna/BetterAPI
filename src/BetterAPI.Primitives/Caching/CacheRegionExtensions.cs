// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text;

namespace BetterAPI.Caching
{
    public static class CacheRegionExtensions
    {
        public static ulong GetSeed<T>(this ICacheRegion<T> cache)
        {
            if (!cache.TryGetValue(nameof(GetSeed), out ulong seed))
                cache.Set(nameof(GetSeed), seed = BitConverter.ToUInt64(Encoding.UTF8.GetBytes(typeof(T).Name), 0));

            return seed;
        }
    }
}