// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace BetterAPI.Caching
{
    public interface ICacheSet
    {
        bool Set(string key, object value);
        bool Set(string key, object value, DateTimeOffset absoluteExpiration);
        bool Set(string key, object value, TimeSpan slidingExpiration);
        bool Set(string key, object value, ICacheDependency dependency);
        bool Set(string key, object value, DateTimeOffset absoluteExpiration, ICacheDependency dependency);
        bool Set(string key, object value, TimeSpan slidingExpiration, ICacheDependency dependency);

        bool Set<T>(string key, T value);
        bool Set<T>(string key, T value, DateTimeOffset absoluteExpiration);
        bool Set<T>(string key, T value, TimeSpan slidingExpiration);
        bool Set<T>(string key, T value, ICacheDependency dependency);
        bool Set<T>(string key, T value, DateTimeOffset absoluteExpiration, ICacheDependency dependency);
        bool Set<T>(string key, T value, TimeSpan slidingExpiration, ICacheDependency dependency);
    }
}