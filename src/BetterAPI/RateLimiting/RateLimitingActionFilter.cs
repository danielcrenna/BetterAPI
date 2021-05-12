// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace BetterAPI.RateLimiting
{
    public sealed class RateLimitingActionFilter : IAsyncActionFilter
    {
        private readonly IStringLocalizer<RateLimitingActionFilter> _localizer;
        private readonly ILogger<RateLimitingActionFilter> _logger;

        public RateLimitingActionFilter(IStringLocalizer<RateLimitingActionFilter> localizer, ILogger<RateLimitingActionFilter> logger)
        {
            _localizer = localizer;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.HttpContext.User.Claims.Any())
            {
                _logger.LogDebug("User is " + context.HttpContext.User.Identity?.Name);
            }
            
            var executed = await next.Invoke();
        }
    }
}