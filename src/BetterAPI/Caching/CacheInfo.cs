// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

namespace BetterAPI.Caching
{
    public struct CacheInfo : ICacheInfo
    {
        public int KeyCount { get; set; }
        public long? SizeLimitBytes { get; set; }
        public long SizeBytes { get; set; }
    }
}