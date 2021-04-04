// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

namespace BetterAPI.Sorting
{
    public class SortOptions : IQueryOptions
    {
        public bool EnabledByDefault { get; set; } = true;
        public bool EmptyClauseIsValid => false;
        public string Operator { get; set; } = "$orderBy";

        public SortClause[] DefaultSort { get; set; } =
            {new SortClause {Field = "Id", Direction = SortDirection.Ascending}};
    }
}