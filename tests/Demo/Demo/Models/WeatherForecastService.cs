// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Demo.Models
{
    public class WeatherForecastService
    {
        private readonly IDictionary<Guid, WeatherForecast> _store =
            new ConcurrentDictionary<Guid, WeatherForecast>();

        private static readonly string[] Summaries =
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public IEnumerable<WeatherForecast> Get()
        {
            return _store.Values;
        }

        public bool TryGetById(Guid id, out WeatherForecast model)
        {
            if(!_store.TryGetValue(id, out var stored))
            {
                model = default;
                return false;
            }

            model = stored;
            return true;
        }

        public bool TryAdd(WeatherForecast model)
        {
            return _store.TryAdd(model.Id, model);
        }
    }
}