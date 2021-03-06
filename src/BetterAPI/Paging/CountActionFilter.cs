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
    public sealed class CountActionFilter : CollectionQueryActionFilter<CountOptions>
    {
        private readonly IStringLocalizer<CountActionFilter> _localizer;
        private readonly IOptionsSnapshot<CountOptions> _options;
        private readonly ILogger<CountActionFilter> _logger;

        public CountActionFilter(IStringLocalizer<CountActionFilter> localizer, IOptionsSnapshot<CountOptions> options, ILogger<CountActionFilter> logger) : 
            base(localizer, options, logger)
        {
            _localizer = localizer;
            _options = options;
            _logger = logger;
        }
        
        public override async Task OnValidRequestAsync(Type underlyingType, StringValues clauses, ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (clauses.Count > 0 && IsCountRequested(clauses))
            {
                context.HttpContext.Items[Constants.CountContextKey] = true;
            }

            ActionExecutedContext executed = await next.Invoke();

            int? totalCount = default;

            // Check if the query is from a continuation token, and pass its preference if it is
            if (context.HttpContext.Items.TryGetValue(Constants.QueryContextKey, out var queryValue) && queryValue is ResourceQuery query && query.CountTotalRows)
            {
                totalCount = query.TotalRows;
            }
            
            if (!executed.HttpContext.Items.ContainsKey(Constants.CountContextKey))
            {
                // the underlying store handled the request, and the total count is available
                if (executed.HttpContext.Items.TryGetValue(Constants.CountResultContextKey, out var countResult) && countResult is int countResultValue)
                {
                    totalCount = countResultValue;
                }
            }

            executed.HttpContext.Items.Remove(Constants.CountContextKey);

            if (executed.Result is OkObjectResult result && !(result.Value is ProblemDetails))
            {
                if (!totalCount.HasValue)
                {
                    _logger.LogWarning(_localizer.GetString(
                        "Result set count operation has fallen back to object-level sub-selection. " +
                        "This means that paging was not performed by the underlying data store, and is not " +
                        "likely consistent across an entire collection."));
                }

                var body = executed.GetResultBody(result, out var settable);
                if (settable && body is IEnumerable enumerable)
                {
                    var type = typeof(CountMany<>).MakeGenericType(underlyingType!);
                    var envelope = Activator.CreateInstance(type, enumerable, totalCount ?? enumerable.Cast<object>().Count());
                    result.Value = envelope;
                }
            }
        }

        private static bool IsCountRequested(StringValues clauses)
        {
            foreach (var clause in clauses)
            {
                if (string.IsNullOrWhiteSpace(clause))
                    continue;

                return clause.Equals("true", StringComparison.OrdinalIgnoreCase) || 
                       int.TryParse(clause, out var countAsNumber) && countAsNumber == 1 || 
                       clause.Equals("yes", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}