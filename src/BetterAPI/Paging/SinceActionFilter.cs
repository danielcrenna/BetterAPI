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

namespace BetterAPI.Paging
{
    public sealed class SinceActionFilter : CollectionQueryActionFilter<SinceOptions>
    {
        private readonly IOptionsSnapshot<SinceOptions> _options;

        public SinceActionFilter(IStringLocalizer<SinceActionFilter> localizer, IOptionsSnapshot<SinceOptions> options, ILogger<SinceActionFilter> logger) : 
            base(localizer, options, logger)
        {
            _options = options;
        }
        
        public override async Task OnValidRequestAsync(Type underlyingType, StringValues clauses, ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (clauses.Count != 1 || !int.TryParse(clauses[0], out var since))
            {
                await next.Invoke();
                return;
            }

            context.HttpContext.Items[Constants.SinceContextKey] = since;

            var executed = await next.Invoke();

            // FIXME: add fallback and warning
        }
    }
}