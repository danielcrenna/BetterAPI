// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BetterAPI.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

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
            if (!context.IsCollectionQuery(out var underlyingType))
            {
                _logger.LogDebug("Skipping {ActionFilter} because this request is not a query", GetType().Name);
                await next();
                return;
            }

            var executed = await next();

            if (executed.Result is ObjectResult result && !(result.Value is ProblemDetails))
            {
                var body = executed.GetResultBody(result, out var settable);

                if (settable)
                {
                    if (executed.HttpContext.Items.TryGetValue(Constants.QueryContextKey, out var queryValue) && queryValue is ResourceQuery query)
                    {
                        var offset = query.PageOffset.GetValueOrDefault();
                        var size = query.PageSize.GetValueOrDefault(_options.Value.MaxPageSize.DefaultPageSize);
                        var maxSize = query.TotalRows.GetValueOrDefault();

                        var hasNextPage = offset + size < maxSize;
                        var hasPrevPage = offset > 0;

                        if (hasNextPage)
                        { 
                            var nextLinkType = typeof(NextLinkAnnotated<>).MakeGenericType(body.GetType());
                            var nextPageHash = _store.BuildNextLinkForQuery(underlyingType!, query);
                            var nextLink = $"{context.HttpContext.Request.GetDisplayUrlNoQueryString()}/nextPage/{nextPageHash}";
                            var page = Activator.CreateInstance(nextLinkType, body, nextLink);
                            result.Value = page;
                        }

                        if (_options.Value.AppendPagingLinkRelations)
                        {
                            AppendPageLinkRelations(executed, hasNextPage, hasPrevPage, size, offset, maxSize);
                        }
                    }
                }
            }
        }

        private void AppendPageLinkRelations(ActionContext context, bool hasNextPage, bool hasPrevPage, int size, int offset, int maxSize)
        {
            Dictionary<string, StringValues> queryInfo;
            if (!string.IsNullOrWhiteSpace(context.HttpContext.Request.QueryString.Value))
            {
                queryInfo = QueryHelpers.ParseNullableQuery(context.HttpContext.Request.QueryString.Value)
                            ?? new Dictionary<string, StringValues>(4);
            }
            else
            {
                queryInfo = new Dictionary<string, StringValues>(4);
            }

            queryInfo[_options.Value.Skip.Operator] = "0";
            queryInfo[_options.Value.Top.Operator] = size.ToString();
            context.HttpContext.AppendLinkRelation(queryInfo, "first");

            if (hasNextPage)
            {
                queryInfo[_options.Value.Skip.Operator] = (offset + size).ToString();
                context.HttpContext.AppendLinkRelation(queryInfo, "next");
            }

            if (hasPrevPage)
            {
                queryInfo[_options.Value.Skip.Operator] = Math.Max(0, offset - size).ToString();
                context.HttpContext.AppendLinkRelation(queryInfo, "prev");
            }

            queryInfo[_options.Value.Skip.Operator] = (maxSize - size).ToString();
            context.HttpContext.AppendLinkRelation(queryInfo, "last");
        }
    }
}