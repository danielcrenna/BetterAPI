// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.ComponentModel.DataAnnotations;
using BetterAPI;
using Demo.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Demo.Controllers
{
    [Display(Description = "Manages operations for weather forecast resources")]
    public class WeatherForecastController : ResourceController<WeatherForecast>
    {
        public WeatherForecastController(WeatherForecastService service, IEventBroadcaster events, IOptionsSnapshot<ApiOptions> options, ILogger<WeatherForecastController> logger) : 
            base(service, events, options, logger) { }
    }
}