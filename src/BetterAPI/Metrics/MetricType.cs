// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace BetterAPI.Metrics
{
    [Flags]
    public enum MetricType
    {
        Gauge = (byte) 1u << 0,
        Counter = (byte) 1u << 1,
        Meter = (byte) 1u << 2,
        Histogram = (byte) 1u << 3,
        Timer = (byte) 1u << 4,

        None = 0,
        All = 0xFF
    }
}