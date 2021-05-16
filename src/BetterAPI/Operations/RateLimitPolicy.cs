// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using BetterAPI.Operations.Internal;

namespace BetterAPI.Operations
{
    /// <summary>
    ///     A rate limit policy, for use with <see cref="PushQueue{T}" />
    /// </summary>
    public class RateLimitPolicy
    {
        public bool Enabled { get; set; }
        public int Occurrences { get; set; }
        public TimeSpan TimeUnit { get; set; }
    }
}