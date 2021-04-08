// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

namespace BetterAPI.Metrics
{
    public static class Constants
    {
        public static readonly double[] Percentiles = {0.5, 0.75, 0.95, 0.98, 0.99, 0.999};

        public static class Categories
        {
            public const string Metrics = "Metrics";
        }
    }
}