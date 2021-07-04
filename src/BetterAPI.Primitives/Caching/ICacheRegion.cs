// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace BetterAPI.Caching
{
    // ReSharper disable once UnusedTypeParameter
    public interface ICacheRegion<TRegion>
    {
        bool TryGetValue<TValue>(in string key, out TValue value);
        TValue Get<TValue>(in string key);
        void Set<TValue>(in string key, TValue value, in TimeSpan ttl);
        void Set<TValue>(in string key, TValue value);
        void Clear();
    }
}