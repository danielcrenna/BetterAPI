// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace BetterAPI.Caching
{
    internal sealed class CacheRegion<TRegion> : ICacheRegion<TRegion>
    {
        private readonly Func<DateTimeOffset> _timestamps;
        private readonly ICache _cache;

        public CacheRegion(Func<DateTimeOffset> timestamps, ICache cache)
        {
            _timestamps = timestamps;
            _cache = cache;
        }

        public bool TryGetValue<TValue>(in string key, out TValue value)
        {
            var result = _cache.Get<TValue>(GetCacheKey(key));
            if (result == null)
            {
                value = default!;
                return false;
            }
            value = result;
            return true;
        }

        public TValue Get<TValue>(in string key) => _cache.Get<TValue>(GetCacheKey(key));
        public void Set<TValue>(in string key, TValue value, in TimeSpan ttl) => _cache.Set(GetCacheKey(key), value, _timestamps().Add(ttl));
        public void Set<TValue>(in string key, TValue value) => _cache.Set(GetCacheKey(key), value);
        public void Clear() => _cache.RemoveAll(GetCacheKey(string.Empty));

        private static string GetCacheKey(string key) => $"{typeof(TRegion).Name}:{key}";
    }
}