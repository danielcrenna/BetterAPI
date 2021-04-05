// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

namespace BetterAPI.DeltaQueries
{
    public sealed class DeltaQueryOptions : IQueryOptions
    {
        public bool HasDefaultBehaviorWhenMissing => false;
        public bool EmptyClauseIsValid => true;
        public string Operator { get; set; } = "$delta";
    }
}