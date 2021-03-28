// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using BetterAPI.Caching;
using BetterAPI.DeltaQueries;
using BetterAPI.Prefer;
using BetterAPI.Sorting;

namespace BetterAPI
{
    public sealed class ApiOptions
    {
        public string ApiName { get; set; } = "BetterAPI";
        public string ApiVersion { get; set; } = "v1";
        public string ApiServer { get; set; } = $"BetterAPI-{typeof(ApiOptions).Assembly.GetName().Version}";

        public CacheOptions Cache { get; set; } = new CacheOptions();
        public SortOptions Sort { get; set; } = new SortOptions();
        public PreferOptions Prefer { get; set; } = new PreferOptions();
        public DeltaQueryOptions DeltaQueries { get; set; } = new DeltaQueryOptions();
        public ProblemDetailsOptions ProblemDetails { get; set; } = new ProblemDetailsOptions();
    }
}