// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Threading;
using BetterAPI.Data;
using BetterAPI.Data.Sqlite;
using BetterAPI.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Demo.Tests
{
    // ReSharper disable once UnusedMember.Global
    public class WeatherForecastControllerSingleItemStoreTests : GivenASingleItemStore<SqliteResourceDataService<WeatherForecastV1>, WeatherForecastV1, Startup>
    {
        private static readonly Guid StableId = Guid.Parse("0F2F5096-C1D8-457C-A55C-04D3663FAD78");

        public WeatherForecastControllerSingleItemStoreTests(ITestOutputHelper output, WebApplicationFactory<Startup> factory) :
            base("/WeatherForecasts", Populate, output, factory)
        {
            Id = StableId;
        }

        private static void Populate(SqliteResourceDataService<WeatherForecastV1> service)
        {
            var model = new WeatherForecastV1
            {
                Id = StableId,
                Date = DateTime.Now,
                Summary = "Chilly",
                TemperatureC = 0
            };
            Assert.True(service.TryAdd(model, out _, CancellationToken.None));
        }
    }
}