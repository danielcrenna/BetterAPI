// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

namespace BetterAPI.Metrics
{
    public interface IDistributed
    {
        long Count { get; }
        double Max { get; }
        double Min { get; }
        double Mean { get; }
        double StdDev { get; }
        double[] Percentiles(params double[] percentiles);
    }
}