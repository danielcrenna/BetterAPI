// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

namespace BetterAPI.Paging
{
    public sealed class MaxPageSizeOptions : IQueryOptions
    {
        public bool HasDefaultBehaviorWhenMissing { get; set; } = true;
        public bool EmptyClauseIsValid => false;
        public string Operator { get; set; } = "$maxpagesize";
        public int DefaultPageSize { get; set; } = 20;
        public int MaxPageSize { get; set; } = 100;
    }
}