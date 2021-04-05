// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace BetterAPI.Filtering
{
    public sealed class FilterQueryActionFilter : CollectionQueryActionFilter<FilterOptions>
    {
        private readonly IOptionsSnapshot<FilterOptions> _options;
        private readonly ILogger<FilterQueryActionFilter> _logger;

        public FilterQueryActionFilter(IOptionsSnapshot<FilterOptions> options, ILogger<FilterQueryActionFilter> logger) : 
            base(options, logger)
        {
            _options = options;
            _logger = logger;
        }

        public override async Task OnValidRequestAsync(Type underlyingType, StringValues clauses, ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // FIXME: support nested clauses

            var filterMap = new List<(string, FilterOperator, string?)>(clauses.Count);

            foreach (var clause in clauses)
            {
                var tokens = clause.Split(new[] {' '},
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (tokens.Length == 0)
                    continue; // (FIXME: add a validation error?)

                if (tokens.Length == 3)
                {
                    var name = tokens[0].ToUpperInvariant();
                    var @operator = tokens[1].ToUpperInvariant();
                    var value = tokens[2].Trim('\'').ToUpperInvariant();

                    switch (@operator)
                    {
                        case "EQ":
                            filterMap.Add((name, FilterOperator.Equal, value));
                            break;
                    }
                }
            }

            context.HttpContext.Items.Add(Constants.FilterOperationContextKey, filterMap);
            
            var executed = await next();

            context.Result = executed.Result; // is this needed to pass down to the next filter?

            if (!context.HttpContext.Items.ContainsKey(Constants.FilterOperationContextKey))
                return; // the underlying store handled the filter request

            context.HttpContext.Items.Remove(Constants.FilterOperationContextKey);

            // FIXME: do memory-based filter (or not? is it valuable or not?)

            _logger?.LogWarning("Filter operation has fallen back to object-level filtering. " +
                                "This means that filtering was not performed by the underlying data store, and is not likely consistent across an entire collection.");
        }
    }
}