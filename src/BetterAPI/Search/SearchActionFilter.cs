// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace BetterAPI.Search
{
    public sealed class SearchActionFilter : CollectionQueryActionFilter<SearchOptions>
    {
        public SearchActionFilter(IStringLocalizer<SearchActionFilter> localizer, IOptionsSnapshot<SearchOptions> options, ILogger<SearchActionFilter> logger) :
            base(localizer, options, logger)
        {
        }

        public override async Task OnValidRequestAsync(Type underlyingType, StringValues clauses, ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            context.HttpContext.Items[Constants.SearchOperationContextKey] = clauses[0];
            
            var executed = await next.Invoke();

            // FIXME: add fallback and warning
        }
    }
}