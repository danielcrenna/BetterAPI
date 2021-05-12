// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BetterAPI
{
    [ApiController]
    public abstract class ResourceController : ControllerBase
    {
        private readonly IStringLocalizer<ResourceController> _localizer;

        protected readonly IOptionsSnapshot<ApiOptions> Options;
        protected readonly ILogger<ResourceController> Logger;

        protected ResourceController(IStringLocalizer<ResourceController> localizer, IOptionsSnapshot<ApiOptions> options, ILogger<ResourceController> logger)
        {
            _localizer = localizer;
            Options = options;
            Logger = logger;
        }

        protected IActionResult BadRequestWithDetails(string details, params object[] arguments)
        {
            return StatusCodeWithProblemDetails(StatusCodes.Status400BadRequest, "Bad Request", details, arguments);
        }

        protected IActionResult InternalServerErrorWithDetails(string details, params object[] arguments)
        {
            return StatusCodeWithProblemDetails(StatusCodes.Status500InternalServerError, "Internal Server Error",
                details, arguments);
        }

        protected IActionResult PayloadTooLargeWithDetails(string details, params object[] arguments)
        {
            return StatusCodeWithProblemDetails(StatusCodes.Status413PayloadTooLarge, "Payload Too Large", details, arguments);
        }

        protected IActionResult GoneWithDetails(string details, params object[] arguments)
        {
            return StatusCodeWithProblemDetails(StatusCodes.Status410Gone, "Gone", details, arguments);
        }

        protected IActionResult NotFoundWithDetails(string details, params object[] arguments)
        {
            return StatusCodeWithProblemDetails(StatusCodes.Status404NotFound, "Not Found", details, arguments);
        }

        protected IActionResult SeeOtherWithDetails(string details, params object[] arguments)
        {
            return StatusCodeWithProblemDetails(StatusCodes.Status303SeeOther, "See Other", details, arguments);
        }

        protected IActionResult UnsupportedMediaTypeWithDetails(string details, params object[] arguments)
        {
            return StatusCodeWithProblemDetails(StatusCodes.Status415UnsupportedMediaType, "Unsupported Media Type", details, arguments);
        }

        protected IActionResult StatusCodeWithProblemDetails(int statusCode, string statusDescription, string details, params object[] arguments)
        {
            return StatusCode(statusCode, new ProblemDetails
            {
                Status = statusCode,
                Type = $"{Options.Value.ProblemDetails.BaseUrl}{statusCode}",
                Title = _localizer.GetString(statusDescription),
                Detail = _localizer.GetString(details, arguments),
                Instance = Request.Path
            });
        }
    }
}