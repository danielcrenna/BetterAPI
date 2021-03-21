// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.ComponentModel.DataAnnotations;

namespace Demo.Models
{
    public class WeatherForecast
    {
        public WeatherForecast()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>
        ///     The forecast's unique ID
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        ///     The date and time of the predicted forecast
        /// </summary>
        [Required]
        public DateTime Date { get; set; }

        /// <summary>
        ///     The temperature in degrees Celsius
        /// </summary>
        [Required]
        public int TemperatureC { get; set; }

        /// <summary>
        ///     The temperature in degrees Fahrenheit
        /// </summary>
        public int TemperatureF => 32 + (int) (TemperatureC / 0.5556f);

        /// <summary>
        ///     A human-readable summary of the temperature
        ///     <example>Chilly</example>
        /// </summary>
        [Required]
        public string? Summary { get; set; }
    }
}