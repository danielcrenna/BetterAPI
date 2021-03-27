// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Demo.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Demo.Tests
{
    public class WeatherControllerSingleItemStoreTests : GivenAPopulatedStore<WeatherForecastService>
    {
        private static readonly Guid StableId = Guid.Parse("0F2F5096-C1D8-457C-A55C-04D3663FAD78");

        public WeatherControllerSingleItemStoreTests(ITestOutputHelper output, WebApplicationFactory<Startup> factory) : base("/WeatherForecasts", Populate, output, factory)
        {
            _id = StableId;
        }

        private static void Populate(WeatherForecastService service)
        {
            var model = new WeatherForecast
            {
                Id = StableId,
                Date = DateTime.Now,
                Summary = "Chilly",
                TemperatureC = 0
            };
            Assert.True(service.TryAdd(model));
        }
    }
}