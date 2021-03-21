﻿using System;

namespace BetterAPI.Guidelines.Caching
{
    public interface ICache : ICacheSet, ICacheAdd, ICacheReplace
    {
        object Get(string key, TimeSpan? timeout = null);
        object GetOrAdd(string key, Func<object> add = null, TimeSpan? timeout = null);
        object GetOrAdd(string key, object add = null, TimeSpan? timeout = null);

        T Get<T>(string key, TimeSpan? timeout = null);
        T GetOrAdd<T>(string key, Func<T> add = null, TimeSpan? timeout = null);
        T GetOrAdd<T>(string key, T add = default, TimeSpan? timeout = null);

        void Remove(string key);
    }
}