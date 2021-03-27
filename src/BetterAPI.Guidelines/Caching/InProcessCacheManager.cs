using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BetterAPI.Guidelines.Caching
{
    public abstract class InProcessCacheManager : ICacheManager
    {
        protected readonly IMemoryCache Cache;
        protected readonly IOptions<CacheOptions> Options;

        protected InProcessCacheManager(IOptions<CacheOptions> options, Func<DateTimeOffset> timestamps)
        {
            MemoryCacheOptions memoryCacheOptions = new MemoryCacheOptions
            {
                CompactionPercentage = 0.05,
                ExpirationScanFrequency = TimeSpan.FromMinutes(1.0),
                Clock = new DelegatedSystemClock(timestamps)
            };
            Cache = new MemoryCache(memoryCacheOptions);
            Options = options;
        }

        #region ICacheManager

        public int KeyCount
        {
            get
            {
                if (!(Cache is MemoryCache memory))
                    return 0;
                var getCount = typeof(MemoryCache).GetProperty(nameof(MemoryCache.Count), BindingFlags.Instance | BindingFlags.Public);
                return (int) (getCount?.GetValue(memory) ?? 0);
            }
        }

        public long SizeBytes => GetApproximateMemorySize();

        public IEnumerable<string> IntrospectKeys()
        {
            var entriesField = typeof(MemoryCache).GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance);
            var entries = entriesField?.GetValue(Cache);
            if (entries is not IDictionary)
                yield break;

            var dictionary = (IDictionary) entries;
            foreach (var key in dictionary.Keys)
                if(key != default)
                    yield return key.ToString() ?? string.Empty;
        }

        private long GetApproximateMemorySize()
        {
            var entriesField = typeof(MemoryCache).GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance);
            var entries = entriesField?.GetValue(Cache);
            if (entries is not IDictionary)
                return 0;

            var sum = 0L;
            var dictionary = (IDictionary) entries;
            foreach (var entry in dictionary.Values)
                sum += entry.GetMemorySize();
            return sum;
        }

        private long GetUnitlessSize()
        {
            if (!(Cache is MemoryCache memory))
                return 0L;
            var getSize = typeof(MemoryCache).GetProperty("Size", BindingFlags.Instance | BindingFlags.NonPublic);
            return (long) (getSize?.GetValue(memory) ?? 0L);
        }

        public long? SizeLimitBytes => Options.Value.MaxSizeBytes;

        public void Clear()
        {
            if (Cache is MemoryCache memory)
                memory.Compact(1);
        }

        #endregion
    }
}