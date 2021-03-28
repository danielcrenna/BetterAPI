// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using BetterAPI.Extensions;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BetterAPI.DeltaQueries
{
    public sealed class DeltaQueryActionFilter : IAsyncActionFilter
    {
        private readonly IOptionsSnapshot<DeltaQueryOptions> _options;

        public DeltaQueryActionFilter(IOptionsSnapshot<DeltaQueryOptions> options)
        {
            _options = options;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!IsValidForRequest(context, out var underlyingType) || underlyingType == null)
            {
                await next.Invoke();
                return;
            }

            var executed = await next.Invoke();

            // FIXME:
            // "Note: If the collection is paginated the deltaLink will only be present on the final page but MUST reflect any changes to the data returned across all pages."

            if (executed.Result is ObjectResult result)
            {
                var body = executed.GetResultBody(result, out var settable);

                if (settable)
                {
                    var deltaType = typeof(DeltaAnnotated<>).MakeGenericType(body.GetType());
                    var deltaLink = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(context.HttpContext.Request.GetEncodedUrl()));
                    
                    // FIXME: Instancing.CreateInstance will crash on DeltaAnnotated<> and Envelope<>,
                    //        so we have to use manual reflection until that is resolved
                    // var delta = Instancing.CreateInstance(typeof(DeltaAnnotated<>).MakeGenericType(deltaType), body, deltaLink);    
                    var delta = Activator.CreateInstance(deltaType, body, deltaLink);
                    
                    result.Value = delta;
                }
            }
        }

        internal static bool IsValidForAction(ActionDescriptor descriptor) => descriptor.IsCollectionQuery(out _);

        internal bool IsValidForRequest(ActionContext context, out Type? underlyingType) =>
            context.IsCollectionQuery(out underlyingType) &&
            context.HttpContext.Request.Query.TryGetValue(_options.Value.Operator, out _);
    }
}