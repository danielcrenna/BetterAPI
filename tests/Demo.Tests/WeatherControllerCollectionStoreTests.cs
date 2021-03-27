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
    public class WeatherControllerCollectionStoreTests : GivenACollectionStore<WeatherForecastService, WeatherForecast>
    {
        private static readonly Guid GreaterThan = Guid.Parse("0F2F5096-C1D8-457C-A55C-04D3663FAD78");
        private static readonly Guid LessThan = Guid.Parse("08A3C2A0-7786-4CCE-A8AD-29761EA0B95B");

        public WeatherControllerCollectionStoreTests(ITestOutputHelper output, WebApplicationFactory<Startup> factory) :
            base("/WeatherForecasts", Populate, output, factory)
        {
            IdGreaterThanInsertedFirst = GreaterThan;
            IdLessThanInsertedSecond = LessThan;
        }

        private static void Populate(WeatherForecastService service)
        {
            Assert.True(service.TryAdd(new WeatherForecast
            {
                Id = GreaterThan,
                Date = DateTime.Now,
                Summary = "Chilly",
                TemperatureC = 0
            }));

            Assert.True(service.TryAdd(new WeatherForecast
            {
                Id = LessThan,
                Date = DateTime.Now,
                Summary = "Scorching",
                TemperatureC = 0
            }));
        }
    }
}