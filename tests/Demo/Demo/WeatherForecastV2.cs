// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using BetterAPI;

namespace Demo
{
    [ResourceName("WeatherForecast")]
    public class WeatherForecastV2 : WeatherForecastV1
    {
        public List<ReporterV1> Reporters { get; set; }

        public WeatherForecastV2()
        {
            Reporters = new List<ReporterV1>();
        }
    }
}