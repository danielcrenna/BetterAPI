// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Threading.Tasks;
using BetterAPI.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BetterAPI.Enveloping
{
    public sealed class EnvelopeActionFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executed = await next();

            if (executed.Result is ObjectResult result && !(result.Value is ProblemDetails) && context.ActionDescriptor.ReturnsEnumerableType(out var collectionType))
            {
                var body = executed.GetResultBody(result, out var settable);
                if (settable && !(body is IEnveloped))
                {
                    var type = typeof(Envelope<>).MakeGenericType(collectionType!);
                    var envelope = Activator.CreateInstance(type, body);
                    result.Value = envelope;
                }
            }
        }
    }
}