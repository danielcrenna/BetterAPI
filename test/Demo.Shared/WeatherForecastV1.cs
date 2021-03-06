// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.ComponentModel.DataAnnotations;
using BetterAPI;
using BetterAPI.DataProtection;
using BetterAPI.Validation;

namespace Demo
{
    [ResourceName("WeatherForecast")]
    [Display(Description = "A weather forecast.")]
    public class WeatherForecastV1 : IResource
    {
        [Required]
        [Display(Description = "The forecast's unique ID")]
        public Guid Id { get; set; }

        [Required]
        [LastModified]
        [Display(Description = "The date and time of the predicted forecast", Order = 1)]
        public DateTime Date { get; set; }

        [Required]
        [Display(Description = "The temperature in degrees Celsius", Order = 2)]
        public int TemperatureC { get; set; }

        [Display(Description = "The temperature in degrees Fahrenheit", Order = 3)]
        public int TemperatureF => 32 + (int) (TemperatureC / 0.5556f);

        [Required]
        [Display(Description = "A human-readable summary of the temperature", Prompt = "Chilly", Order = 4)]
        [OneOf("Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching")]
        public string? Summary { get; set; }

        [ProtectedByPolicy("TopSecret")]
        [Display(Description = "A secret message, only available to those with access.", Order = 5)]
        public string? SecretMessage { get; set; }
    }
}