// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

namespace BetterAPI.Metrics
{
    public class MetricsOptions
    {
        public int SampleTimeoutSeconds { get; set; } = 5;
        public int EventSourceIntervalSeconds { get; set; } = 1;
    }
}