// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace BetterAPI.Filtering
{
    public sealed class FilterActionFilter : CollectionQueryActionFilter<FilterOptions>
    {
        private readonly IOptionsSnapshot<FilterOptions> _options;

        public FilterActionFilter(IOptionsSnapshot<FilterOptions> options, ILogger<FilterActionFilter> logger) : 
            base(options, logger)
        {
            _options = options;
        }

        public override async Task OnValidRequestAsync(Type underlyingType, StringValues clauses, ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await next.Invoke();
        }
    }
}