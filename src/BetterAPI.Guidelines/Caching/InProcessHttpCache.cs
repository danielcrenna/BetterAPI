// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace BetterApi.Guidelines.Caching
{
    public class InProcessHttpCache : InProcessCacheManager, IHttpCache
    {
        public InProcessHttpCache(IOptions<ApiOptions> options, Func<DateTimeOffset> timestamps) : base(options, timestamps) { }

        public bool TryGetETag(string key, out string? etag)
        {
            var cacheKey = $"{key}_{HeaderNames.ETag}";
            if (Cache.TryGetValue<byte[]>(cacheKey, out var buffer))
            {
                etag = Encoding.UTF8.GetString(buffer);
                return true;
            }

            etag = default;
            return false;
        }

        public bool TryGetLastModified(string key, out DateTimeOffset lastModified)
        {
            if (!Cache.TryGetValue($"{key}_{HeaderNames.LastModified}", out lastModified))
            {
                return true;
            }

            lastModified = default;
            return false;
        }

        public void Save(string key, string etag)
        {
            Cache.Set($"{key}_{HeaderNames.ETag}", Encoding.UTF8.GetBytes(etag));
        }

        public void Save(string key, DateTimeOffset lastModified)
        {
            Cache.Set($"{key}_{HeaderNames.LastModified}",
                Encoding.UTF8.GetBytes(lastModified.ToString("d")));
        }
    }
}