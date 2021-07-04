// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BetterAPI;
using BetterAPI.DataProtection;

namespace Demo
{
    [ResourceName("WeatherForecast")]
    public class WeatherForecastV2 : IResource
    {
        [Required]
        [Display(Description = "The forecast's unique ID")]
        public Guid Id { get; set; }

        [Required]
        [LastModified]
        [Display(Description = "The date and time of the predicted forecast")]
        public DateTime Date { get; set; }

        [Required]
        [Display(Description = "The temperature in degrees Celsius")]
        public int TemperatureC { get; set; }

        [Display(Description = "The temperature in degrees Fahrenheit")]
        public int TemperatureF => 32 + (int) (TemperatureC / 0.5556f);

        [Required]
        [Display(Description = "An index to an external summary", Prompt = "1")]
        public int? Summary { get; set; }

        [ProtectedByPolicy("TopSecret")]
        public string? SecretMessage { get; set; }

        public List<ReporterV1> Reporters { get; set; }

        public WeatherForecastV2()
        {
            Reporters = new List<ReporterV1>();
        }
    }
}