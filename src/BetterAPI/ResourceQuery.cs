// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using BetterAPI.Reflection;
using BetterAPI.Sorting;

namespace BetterAPI
{
    public sealed class ResourceQuery
    {
        public int? PageOffset { get; set; } = 0;
        public int? PageSize { get; set; } = 0;
        public int? MaxPageSize { get; set; } = default;

        public bool CountTotalRows { get; set; } = false;
        public int? TotalRows { get; set; } = default;

        public List<string>? Fields { get; set; } = default;
        public List<(AccessorMember, SortDirection)>? Sorting { get; set; } = default;
        public string? SearchQuery { get; set; } = default;
    }
}