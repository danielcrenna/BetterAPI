﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Localization;

namespace BetterAPI.HealthChecks
{
    public sealed class HealthCheckStartupFilter : IStartupFilter
    {
        private readonly IStringLocalizer<HealthCheckStartupFilter> _localizer;
        private readonly HealthCheckService _service;
        
        public HealthCheckStartupFilter(IStringLocalizer<HealthCheckStartupFilter> localizer, HealthCheckService service)
        {
            _localizer = localizer;
            _service = service;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            var report = _service.CheckHealthAsync(r => r.Tags.Contains("startup")).GetAwaiter().GetResult();

            return report.Status == HealthStatus.Unhealthy
                ? throw new ApplicationException(_localizer.GetString("Application failed to start due to failing startup health checks."))
                : next;
        }
    }
}
