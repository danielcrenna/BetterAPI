// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BetterAPI.Caching
{
    public class InProcessHttpCache : InProcessCacheManager, IHttpCache
    {
        public InProcessHttpCache(IOptions<CacheOptions> options, Func<DateTimeOffset> timestamps) : base(options,
            timestamps)
        {
        }

        public bool TryGetETag(string cacheKey, out string? etag)
        {
            if (Cache.TryGetValue<byte[]>(cacheKey, out var buffer))
            {
                etag = Encoding.UTF8.GetString(buffer);
                return true;
            }

            etag = default;
            return false;
        }

        public bool TryGetLastModified(string cacheKey, out DateTimeOffset lastModified)
        {
            if (!Cache.TryGetValue(cacheKey, out lastModified)) return true;

            lastModified = default;
            return false;
        }

        public bool Save(string displayUrl, string etag)
        {
            var cacheKey = BuildETagCacheKey(displayUrl, etag);
            if (Cache.TryGetValue(cacheKey, out _))
                return false;
            Cache.Set(cacheKey, Encoding.UTF8.GetBytes(etag));
            return true;
        }

        public bool Save(string displayUrl, DateTimeOffset lastModified)
        {
            var cacheKey = BuildLastModifiedCacheKey(displayUrl, lastModified);
            if (Cache.TryGetValue(cacheKey, out _))
                return false;
            Cache.Set(cacheKey, Encoding.UTF8.GetBytes(lastModified.ToString("d")));
            return true;
        }

        private static string BuildLastModifiedCacheKey(string displayUrl, DateTimeOffset lastModified)
        {
            return $"{displayUrl}_{ApiHeaderNames.LastModified}_{lastModified:R}";
        }

        private static string BuildETagCacheKey(string displayUrl, string etag)
        {
            return $"{displayUrl}_{ApiHeaderNames.ETag}_{etag}";
        }
    }
}