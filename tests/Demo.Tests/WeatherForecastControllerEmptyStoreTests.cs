// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using BetterAPI.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace Demo.Tests
{
    // ReSharper disable once UnusedMember.Global
    public class WeatherForecastControllerEmptyStoreTests : GivenAnEmptyStore<WeatherForecast, Startup>
    {
        public WeatherForecastControllerEmptyStoreTests(ITestOutputHelper output, WebApplicationFactory<Startup> factory) :
            base("/WeatherForecasts", output, factory)
        {
        }
    }
}