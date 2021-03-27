// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace BetterAPI.Prefer
{
    internal sealed class PreferActionFilter : IAsyncActionFilter
    {
        private readonly IOptionsSnapshot<PreferOptions> _options;

        public PreferActionFilter(IOptionsSnapshot<PreferOptions> options)
        {
            _options = options;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var minimal = false;

            if (context.HttpContext.Request.Headers.TryGetValue(ApiHeaderNames.Prefer, out var prefer))
            {
                foreach (var _ in prefer.Where(token =>
                    token.Equals(Constants.Prefer.ReturnMinimal, StringComparison.OrdinalIgnoreCase)))
                {
                    context.HttpContext.Response.Headers.Add(ApiHeaderNames.PreferenceApplied,
                        Constants.Prefer.ReturnMinimal);
                    minimal = true;
                }

                // this is the current default, but we still want to indicate the preference was followed
                foreach (var _ in prefer.Where(token =>
                    token.Equals(Constants.Prefer.ReturnRepresentation, StringComparison.OrdinalIgnoreCase)))
                {
                    context.HttpContext.Response.Headers.Add(ApiHeaderNames.PreferenceApplied,
                        Constants.Prefer.ReturnRepresentation);
                    minimal = false;
                }
            }
            else
            {
                minimal = _options.Value.DefaultReturn switch
                {
                    PreferReturn.Minimal => true,
                    _ => false
                };
            }

            var executed = await next.Invoke();

            if (executed.Result is ObjectResult result)
                if (minimal)
                {
                    executed.HttpContext.Items[Constants.ObjectResultValue] = result.Value;
                    result.Value = default;
                    executed.HttpContext.Response.StatusCode = (int) HttpStatusCode.NoContent;
                }
        }
    }
}