// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using BetterAPI.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace BetterAPI.Paging
{
    public sealed class MaxPageSizeActionFilter : CollectionQueryActionFilter<MaxPageSizeOptions>
    {
        private readonly IStringLocalizer<MaxPageSizeActionFilter> _localizer;
        private readonly IOptionsSnapshot<PagingOptions> _pagingOptions;
        private readonly IOptionsSnapshot<MaxPageSizeOptions> _options;
        private readonly ILogger<MaxPageSizeActionFilter> _logger;

        public MaxPageSizeActionFilter(IStringLocalizer<MaxPageSizeActionFilter> localizer, IOptionsSnapshot<PagingOptions> pagingOptions, IOptionsSnapshot<MaxPageSizeOptions> options, ILogger<MaxPageSizeActionFilter> logger) : 
            base(localizer, options, logger)
        {
            _localizer = localizer;
            _pagingOptions = pagingOptions;
            _options = options;
            _logger = logger;
        }
        
        public override async Task OnValidRequestAsync(Type underlyingType, StringValues clauses, ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var maxPageSize = _pagingOptions.Value.MaxPageSize.DefaultPageSize;
            if (clauses.Count > 0 && int.TryParse(clauses[0], out var pageSize) && pageSize < maxPageSize)
                maxPageSize = pageSize;

            context.HttpContext.Items[Constants.MaxPageSizeContextKey] = maxPageSize;
            
            var executed = await next.Invoke();

            if (!executed.HttpContext.Items.ContainsKey(Constants.MaxPageSizeContextKey))
                return; // the underlying store handled the sort request

            executed.HttpContext.Items.Remove(Constants.MaxPageSizeContextKey);

            _logger.LogWarning(_localizer.GetString("Max page size operation has fallen back to object-level sub-selection. " +
                               "This means that paging was not performed by the underlying data store, and is not " +
                               "likely consistent across an entire collection."));

            if (executed.Result is OkObjectResult result)
            {
                var body = executed.GetResultBody(result, out var settable);
                if (settable && body is IEnumerable)
                {
                    var takeMethod = typeof(Enumerable)
                        .GetMethod(nameof(Enumerable.Take)) ?? throw new NullReferenceException();

                    result.Value = takeMethod.MakeGenericMethod(underlyingType)
                        .Invoke(body, new[] {body, maxPageSize});
                }
            }
        }
    }
}