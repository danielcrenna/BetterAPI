// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;

namespace BetterAPI.Guidelines
{
    internal sealed class ApiGuidelinesActionFilter : IAsyncActionFilter
    {
        private readonly IDistributedCache _cache;

        public ApiGuidelinesActionFilter(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var minimal = false;

            if (context.HttpContext.Request.Headers.TryGetValue(ApiGuidelines.Headers.Prefer, out var prefer))
            {
                foreach (var _ in prefer.Where(token => token.Equals(ApiGuidelines.Prefer.ReturnMinimal, StringComparison.OrdinalIgnoreCase)))
                {
                    context.HttpContext.Response.Headers.Add(ApiGuidelines.Headers.PreferenceApplied, ApiGuidelines.Prefer.ReturnMinimal);
                    minimal = true;
                }
            }

            var executed = await next.Invoke();

            if (executed.Result is ObjectResult result)
            {
                if (minimal)
                {
                    executed.HttpContext.Items[ApiGuidelines.ObjectResultValue] = result.Value;
                    result.Value = default;
                }
            }
        }
    }
}