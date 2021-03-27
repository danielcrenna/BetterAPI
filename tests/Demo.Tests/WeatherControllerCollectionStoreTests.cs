// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BetterAPI.Guidelines;
using Demo.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;

namespace Demo.Tests
{
    public class WeatherControllerCollectionStoreTests : GivenACollectionStore<WeatherForecastService, WeatherForecast>
    {
        private static readonly Guid StableId = Guid.Parse("0F2F5096-C1D8-457C-A55C-04D3663FAD78");

        public WeatherControllerCollectionStoreTests(ITestOutputHelper output, WebApplicationFactory<Startup> factory) : base("/WeatherForecasts", Populate, output, factory) { }

        private static void Populate(WeatherForecastService service)
        {
            Assert.True(service.TryAdd(new WeatherForecast
            {
                Id = StableId,
                Date = DateTime.Now,
                Summary = "Chilly",
                TemperatureC = 0
            }));

            Assert.True(service.TryAdd(new WeatherForecast
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Now,
                Summary = "Scorching",
                TemperatureC = 0
            }));
        }
    }
}