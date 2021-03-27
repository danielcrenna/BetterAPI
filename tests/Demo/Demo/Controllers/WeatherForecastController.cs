// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using Demo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Demo.Controllers
{
    /// <summary>
    ///     Manages operations for weather forecasts
    /// </summary>
    [ApiController]
    [Route("WeatherForecasts")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly WeatherForecastService _service;
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(WeatherForecastService service, ILogger<WeatherForecastController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpOptions]
        public void GetOptions()
        {
            // See: https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#744-options-and-link-headers
            // OPTIONS allows a client to retrieve information about a resource,
            // at a minimum by returning the Allow header denoting the valid methods for this resource.
            Response.Headers.TryAdd(HeaderNames.Allow, "GET, POST");

            // In addition, services SHOULD include a Link header (see RFC 5988) to point to documentation for the resource in question:
            // Link: <{help}>; rel="help"
        }

        /// <summary> Returns all saved weather forecasts </summary>
        /// <response code="304">The resource was not returned, because it was not modified according to the ETag or LastModifiedDate. </response>
        [HttpGet]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(IEnumerable<WeatherForecast>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status304NotModified)]
        public IActionResult Get()
        {
            return Ok(_service.Get().OrderBy(x => x.Id));
        }

        /// <summary> Returns a saved weather forecast by its unique ID </summary>
        /// <response code="304">The resource was not returned, because it was not modified according to the ETag or LastModifiedDate </response>
        [HttpGet("{id}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(WeatherForecast), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status304NotModified)]
        public IActionResult GetById(Guid id)
        {
            if (!_service.TryGetById(id, out var model))
                return NotFound();

            return Ok(model);
        }

        /// <summary> Creates a new weather forecast </summary>
        /// <param name="model">A new weather forecast </param>
        /// <response code="201">Returns the newly created forecast, or an empty body if a minimal return is preferred </response>
        /// <response code="400">There was an error with the request, and further problem details are available </response>
        /// <response code="412">The resource was not created, because it has unmet pre-conditions </response>
        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
        [ProducesResponseType(typeof(WeatherForecast), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public IActionResult Create([FromBody] WeatherForecast model)
        {
            if (!TryValidateModel(model))
                return BadRequest(ModelState);

            if (model.Id.Equals(Guid.Empty))
                return BadRequestWithDetails("The weather forecast's ID was uninitialized.");

            if (_service.TryGetById(model.Id, out _))
                return BadRequestWithDetails("This weather forecast already exists. Did you mean to update it?");

            //
            // FIXME: The corner-case where we're creating but also have a pre-condition which should block this create operation.
            //        It is unlikely to occur in real life, but technically we should know what the ETag is before we attempt this,
            //        but the LastModifiedDate would always be 'now' since we're not expecting anything to exist.
            
            if (!_service.TryAdd(model))
            {
                _logger.LogError(LogEvents.ErrorSavingWeatherForecast, "Adding weather forecast {Model} failed to save to the underlying data store.", model);
                return InternalServerErrorWithDetails("An unexpected error occurred saving this weather forecast. An error was logged. Please try again later.");
            }

            return Created(Location(), model);

            string Location() => $"{Request.Path}/{model.Id}";
        }

        private IActionResult BadRequestWithDetails(string details) => StatusCodeWithProblemDetails(StatusCodes.Status400BadRequest, "Bad Request", details);

        private IActionResult InternalServerErrorWithDetails(string details) => StatusCodeWithProblemDetails(StatusCodes.Status500InternalServerError, "Internal Server Error", details);

        private IActionResult StatusCodeWithProblemDetails(int statusCode, string statusDescription, string details) => StatusCode(statusCode, new ProblemDetails
        {
            Status = statusCode,
            Type = "https://httpstatuscodes.com/" + statusCode,
            Title = statusDescription,
            Detail = details,
            Instance = Request.Path
        });
    }
}