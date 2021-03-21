// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using BetterApi.Guidelines.Caching;

namespace BetterApi.Guidelines
{
    public sealed class ApiOptions
    {
        public string ApiName { get; set; } = "BetterAPI";
        public string ApiVersion { get; set; } = "v1";
        public CacheOptions Cache { get; set; } = new CacheOptions();
    }
}