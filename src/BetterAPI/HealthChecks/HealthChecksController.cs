// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace BetterAPI.HealthChecks
{
    [Route("api/health")]
    public sealed class HealthChecksController : Controller
    {
        private readonly IOptionsSnapshot<HealthCheckOptions> _options;
        private readonly IStringLocalizer<HealthChecksController> _localizer;
        private readonly HealthCheckService _service;

        public HealthChecksController(IStringLocalizer<HealthChecksController> localizer, HealthCheckService service, IOptionsSnapshot<HealthCheckOptions> options)
        {
            _localizer = localizer;
            _service = service;
            _options = options;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            var filter = HttpContext.Request.Query.TryGetValue("tags", out var tags)
                ? r => r.Tags.IsSupersetOf(tags)
                : _options.Value.Predicate;

            return await GetHealthChecksReportAsync(filter);
        }

        private async Task<IActionResult> GetHealthChecksReportAsync(Func<HealthCheckRegistration, bool> filter)
        {
            var report = await _service.CheckHealthAsync(filter, HttpContext.RequestAborted);

            if (!_options.Value.ResultStatusCodes.TryGetValue(report.Status, out var statusCode))
            {
                throw new InvalidOperationException(
                    _localizer.GetString("No status code mapping found for HealthStatus '{0}'",
                        report.Status));
            }

            HttpContext.Response.StatusCode = statusCode;

            if (!_options.Value.AllowCachingResponses)
            {
                var headers = HttpContext.Response.Headers;
                headers["Cache-Control"] = "no-store, no-cache";
                headers["Pragma"] = "no-cache";
                headers["Expires"] = "Thu, 01 Jan 1970 00:00:00 GMT";
            }

            return Ok(new One<HealthReport> { Value = report });
        }
    }
}
