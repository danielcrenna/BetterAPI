// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using BetterAPI;
using BetterAPI.Sorting;
using BetterAPI.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Demo.Tests
{
    // ReSharper disable once UnusedMember.Global
    public class WeatherForecastControllerCollectionStoreTests : GivenACollectionStore<MemoryResourceDataService<WeatherForecast>, WeatherForecast, Startup>
    {
        public WeatherForecastControllerCollectionStoreTests(ITestOutputHelper output, WebApplicationFactory<Startup> factory) :
            base("/WeatherForecasts", output, factory)
        {
        }

        public override Guid IdGreaterThanInsertedFirst => Guid.Parse("0F2F5096-C1D8-457C-A55C-04D3663FAD78");
        public override Guid IdLessThanInsertedSecond => Guid.Parse("08A3C2A0-7786-4CCE-A8AD-29761EA0B95B");

        public override SortClause AlternateSort()
        {
            return new SortClause {Field = nameof(WeatherForecast.Summary), Direction = SortDirection.Descending};
        }

        public override void Populate(MemoryResourceDataService<WeatherForecast> service)
        {
            Assert.True(service.TryAdd(new WeatherForecast
            {
                Id = IdGreaterThanInsertedFirst,
                Date = DateTime.Now,
                Summary = "Chilly",
                TemperatureC = 0
            }));

            Assert.True(service.TryAdd(new WeatherForecast
            {
                Id = IdLessThanInsertedSecond,
                Date = DateTime.Now,
                Summary = "Scorching",
                TemperatureC = 0
            }));
        }
    }
}