// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace BetterAPI.Caching
{
    public interface ICache : ICacheSet, ICacheAdd, ICacheReplace
    {
        object? Get(string key, TimeSpan? timeout = null);
        object? GetOrAdd(string key, Func<object>? add = null, TimeSpan? timeout = null);
        object? GetOrAdd(string key, object? add = null, TimeSpan? timeout = null);

        T Get<T>(string key, TimeSpan? timeout = null);
        T GetOrAdd<T>(string key, Func<T>? add = default, TimeSpan? timeout = null);
        T GetOrAdd<T>(string key, T add = default!, TimeSpan? timeout = null);

        void Remove(string key);
        void RemoveAll(string prefix);
    }
}