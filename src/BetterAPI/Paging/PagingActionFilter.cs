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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BetterAPI.Paging
{
    public sealed class PagingActionFilter : CollectionActionFilter
    {
        private readonly IPageQueryStore _store;
        private readonly IOptionsSnapshot<PagingOptions> _options;
        private readonly ILogger<PagingActionFilter> _logger;

        public PagingActionFilter(IPageQueryStore store, IOptionsSnapshot<PagingOptions> options, ILogger<PagingActionFilter> logger)             
        {
            _store = store;
            _options = options;
            _logger = logger;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.IsQuery(out var underlyingType))
            {
                _logger?.LogDebug("Skipping {ActionFilter} because this request is not a query", GetType().Name);
                await next();
                return;
            }

            var executed = await next();

            if (executed.Result is ObjectResult result)
            {
                var body = executed.GetResultBody(result, out var settable);

                if (settable)
                {
                    var nextLinkType = typeof(NextLinkAnnotated<>).MakeGenericType(body.GetType());
                    var nextLink = _store.BuildNextLinkForQuery(underlyingType!);
                    
                    // FIXME: Instancing.CreateInstance will crash on NextLinkAnnotated<> and Envelope<>,
                    //        so we have to use manual reflection until that is resolved
                    // var delta = Instancing.CreateInstance(typeof(NextLinkAnnotated<>).MakeGenericType(nextLinkType), body, nextLink);    
                    var page = Activator.CreateInstance(nextLinkType, body, nextLink);
                    
                    result.Value = page;
                }
            }
        }
    }
}