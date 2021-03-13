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

namespace BetterAPI
{
    public class PreferFilter : IAsyncActionFilter
    {
        public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.HttpContext.Request.Headers.TryGetValue(Guidelines.Headers.Prefer, out var prefer))
            {
                foreach (var token in prefer.Where(token => token.Equals(Guidelines.Prefer.ReturnMinimal, StringComparison.OrdinalIgnoreCase)))
                {
                    context.HttpContext.Response.Headers.Add(Guidelines.Headers.PreferenceApplied, Guidelines.Prefer.ReturnMinimal);
                    if (context.Result is ObjectResult result)
                        result.Value = default;
                }
            }

            return Task.CompletedTask;
        }
    }
}