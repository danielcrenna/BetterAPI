// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace BetterAPI.Caching
{
    public interface IHttpCache
    {
        bool TryGetETag(string cacheKey, out string? etag);
        bool TryGetLastModified(string cacheKey, out DateTimeOffset lastModified);
        bool Save(string displayUrl, string etag);
        bool Save(string displayUrl, DateTimeOffset lastModified);
    }
}