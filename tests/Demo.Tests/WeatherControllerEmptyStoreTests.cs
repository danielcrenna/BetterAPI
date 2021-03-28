// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using BetterAPI.Testing;
using Demo.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace Demo.Tests
{
    public class WeatherControllerEmptyStoreTests : GivenAnEmptyStore<WeatherForecast, Startup>
    {
        public WeatherControllerEmptyStoreTests(ITestOutputHelper output, WebApplicationFactory<Startup> factory) :
            base("/WeatherForecasts", output, factory)
        {
        }
    }
}