// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BetterAPI.Search
{
    public sealed class SearchActionFilter : CollectionActionFilter
    {
        private readonly IOptionsSnapshot<SearchOptions> _options;
        private readonly ILogger<SearchActionFilter> _logger;
        private readonly ISearchQueryStore _store;

        public SearchActionFilter(ISearchQueryStore store, IOptionsSnapshot<SearchOptions> options, ILogger<SearchActionFilter> logger)
        {
            _store = store;
            _options = options;
            _logger = logger;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            throw new System.NotImplementedException();
        }
    }
}