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
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace BetterAPI
{
    public abstract class CollectionQueryActionFilter<TOptions> : CollectionActionFilter where TOptions : class, IQueryOptions
    {
        private readonly IStringLocalizer<CollectionQueryActionFilter<TOptions>> _localizer;
        private readonly IOptionsSnapshot<TOptions> _options;
        private readonly ILogger<CollectionQueryActionFilter<TOptions>> _logger;

        protected CollectionQueryActionFilter(IStringLocalizer<CollectionQueryActionFilter<TOptions>> localizer, IOptionsSnapshot<TOptions> options, ILogger<CollectionQueryActionFilter<TOptions>> logger)
        {
            _localizer = localizer;
            _options = options;
            _logger = logger;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!IsValidForRequest(context, out var clauses, out var underlyingType) || underlyingType == null)
            {
                await next.Invoke();
                return;
            }

            var filterType = GetType();
            _logger.LogDebug(_localizer.GetString("Executing {ActionFilter}"), filterType.Name);
            await OnValidRequestAsync(underlyingType, clauses, context, next);
        }

        public abstract Task OnValidRequestAsync(Type underlyingType, StringValues clauses, ActionExecutingContext context, ActionExecutionDelegate next);

        protected bool IsValidForRequest(ActionContext context, out StringValues clauses, out Type? underlyingType)
        {
            if (!context.IsCollectionQuery(out underlyingType))
            {
                _logger.LogDebug(_localizer.GetString("Skipping {ActionFilter} because this request is not a collection query"), GetType().Name);
                clauses = default;
                return false;
            }

            if (!context.HttpContext.Request.Query.TryGetValue(_options.Value.Operator, out clauses) && !_options.Value.HasDefaultBehaviorWhenMissing)
            {
                _logger.LogDebug(_localizer.GetString("Skipping {ActionFilter} because this request does not contain a {Operator} query parameter"), GetType().Name, _options.Value.Operator);
                clauses = default;
                return false;
            }

            if (clauses.Count != 0 || _options.Value.EmptyClauseIsValid)
                return true;

            if (_options.Value.HasDefaultBehaviorWhenMissing)
                return true;

            _logger.LogDebug(_localizer.GetString("Skipping {ActionFilter} because it does not contain any {Operator} values"), GetType().Name, _options.Value.Operator);
            clauses = default;
            return false;
        }
    }
}