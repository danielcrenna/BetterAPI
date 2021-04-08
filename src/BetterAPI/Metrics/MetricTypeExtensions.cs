// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

namespace BetterAPI.Metrics
{
    public static class MetricTypeExtensions
    {
        public static bool HasFlagFast(this MetricType value, MetricType flag)
        {
            return (value & flag) != 0;
        }
    }
}