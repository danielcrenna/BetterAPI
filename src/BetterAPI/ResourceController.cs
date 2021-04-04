﻿// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BetterAPI
{
    [ApiController]
    public abstract class ResourceController : ControllerBase
    {
        protected readonly IOptionsSnapshot<ApiOptions> Options;

        protected readonly ILogger<ResourceController> Logger;

        protected ResourceController(IOptionsSnapshot<ApiOptions> options, ILogger<ResourceController> logger)
        {
            Options = options;
            Logger = logger;
        }

        protected IActionResult BadRequestWithDetails(string details)
        {
            return StatusCodeWithProblemDetails(StatusCodes.Status400BadRequest, "Bad Request", details);
        }

        protected IActionResult InternalServerErrorWithDetails(string details)
        {
            return StatusCodeWithProblemDetails(StatusCodes.Status500InternalServerError, "Internal Server Error",
                details);
        }

        protected IActionResult StatusCodeWithProblemDetails(int statusCode, string statusDescription, string details)
        {
            return StatusCode(statusCode, new ProblemDetails
            {
                Status = statusCode,
                Type = $"{Options.Value.ProblemDetails.BaseUrl}{statusCode}",
                Title = statusDescription,
                Detail = details,
                Instance = Request.Path
            });
        }
    }
}