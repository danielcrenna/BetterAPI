// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using BetterAPI.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace BetterAPI.DeltaQueries
{
    public sealed class DeltaQueryActionFilter : CollectionQueryActionFilter<DeltaQueryOptions>
    {
        private readonly IDeltaQueryStore _store;

        public DeltaQueryActionFilter(IStringLocalizer<DeltaQueryActionFilter> localizer, IDeltaQueryStore store, IOptionsSnapshot<DeltaQueryOptions> options, ILogger<DeltaQueryActionFilter> logger) : 
            base(localizer, options, logger)
        {
            _store = store;
        }
        
        public override async Task OnValidRequestAsync(Type underlyingType, StringValues clauses, ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executed = await next.Invoke();

            // FIXME:
            // "Note: If the collection is paginated the deltaLink will only be present on the final page but MUST reflect any changes to the data returned across all pages."

            if (executed.Result is ObjectResult result)
            {
                var body = executed.GetResultBody(result, out var settable);

                if (settable)
                {
                    var deltaType = typeof(DeltaAnnotated<>).MakeGenericType(body.GetType());
                    var deltaLink = $"{context.HttpContext.Request.GetDisplayUrlNoQueryString()}/{_store.BuildDeltaLinkForQuery(underlyingType)}";
                    
                    // FIXME: Instancing.CreateInstance will crash on DeltaAnnotated<> and Envelope<>,
                    //        so we have to use manual reflection until that is resolved
                    // var delta = Instancing.CreateInstance(typeof(DeltaAnnotated<>).MakeGenericType(deltaType), body, deltaLink);    
                    var delta = Activator.CreateInstance(deltaType, body, deltaLink);
                    
                    result.Value = delta;
                }
            }
        }
    }
}