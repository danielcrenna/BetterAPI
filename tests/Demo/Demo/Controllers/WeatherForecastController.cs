// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Mime;
using Demo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Demo.Controllers
{
    /// <summary>
    ///     Manages operations for weather forecasts
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly IDictionary<Guid, WeatherForecast> Store =
            new ConcurrentDictionary<Guid, WeatherForecast>();

        private static readonly string[] Summaries =
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        /// <summary> Returns all saved weather forecasts </summary>
        [HttpGet]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(IEnumerable<WeatherForecast>), StatusCodes.Status200OK)]
        public IEnumerable<WeatherForecast> Get()
        {
            foreach (var forecast in Store.Values) yield return forecast;
        }

        /// <summary> Returns a saved weather forecast by its unique ID </summary>
        [HttpGet("{id}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(WeatherForecast), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetById(Guid id)
        {
            if (!Store.TryGetValue(id, out var model))
                return NotFound();

            return Ok(model);
        }

        /// <summary> Creates a new weather forecast </summary>
        /// <param name="model">A new weather forecast </param>
        /// <response code="201">Returns the newly created forecast, or an empty body if a minimal return is preferred </response>
        /// <response code="400">There was an error with the request, and further problem details are available </response>
        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(WeatherForecast), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public IActionResult Create([FromBody] WeatherForecast model)
        {
            if (!TryValidateModel(model))
                return BadRequest(ModelState);

            if (model.Id.Equals(Guid.Empty))
                return BadRequestWithDetails("The weather forecast's ID was uninitialized.");

            if (Store.ContainsKey(model.Id))
                return BadRequestWithDetails("This weather forecast already exists. Did you mean to update it?");

            if (!Store.TryAdd(model.Id, model))
            {
                _logger.LogError(LogEvents.ErrorSavingWeatherForecast, "Adding weather forecast {Model} failed to save to the underlying data store.", model);
                return InternalServerErrorWithDetails("An unexpected error occurred saving this weather forecast. An error was logged. Please try again later.");
            }

            return Created(Location(), model);

            string Location() => $"{Request.Path}/{model.Id}";
        }

        private IActionResult BadRequestWithDetails(string details) => StatusCodeWithDetails(StatusCodes.Status500InternalServerError, "Bad Request", details);

        private IActionResult InternalServerErrorWithDetails(string details) => StatusCodeWithDetails(StatusCodes.Status500InternalServerError, "Internal Server Error", details);

        private IActionResult StatusCodeWithDetails(int statusCode, string statusDescription, string details) => StatusCode(statusCode, new ProblemDetails
        {
            Status = statusCode,
            Type = "https://httpstatuscodes.com/" + statusCode,
            Title = statusDescription,
            Detail = details,
            Instance = Request.Path
        });
    }
}