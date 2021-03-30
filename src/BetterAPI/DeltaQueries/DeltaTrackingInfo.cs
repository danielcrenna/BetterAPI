// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace BetterAPI.DeltaQueries
{
    public sealed class DeltaTrackingInfo
    {
        public Type Type { get; set; }
        public DateTimeOffset TrackingDateTime { get; set; }

        public DeltaTrackingInfo(Type type)
        {
            Type = type;
        }
    }
}