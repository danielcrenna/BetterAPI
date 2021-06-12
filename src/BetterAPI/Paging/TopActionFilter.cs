using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace BetterAPI.Paging
{
    public sealed class TopActionFilter : CollectionQueryActionFilter<TopOptions>
    {
        private readonly IOptionsSnapshot<TopOptions> _options;
        private readonly IOptionsSnapshot<MaxPageSizeOptions> _maxPageOptions;

        public TopActionFilter(IStringLocalizer<TopActionFilter> localizer, IOptionsSnapshot<TopOptions> options, IOptionsSnapshot<MaxPageSizeOptions> maxPageOptions, ILogger<TopActionFilter> logger) : 
            base(localizer, options, logger)
        {
            _options = options;
            _maxPageOptions = maxPageOptions;
        }
        
        public override async Task OnValidRequestAsync(Type underlyingType, StringValues clauses, ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var pageSize = _maxPageOptions.Value.DefaultPageSize;
            
            if (clauses.Count == 1 && int.TryParse(clauses[0], out var top))
                pageSize = top;

            context.HttpContext.Items[Constants.TopOperationContextKey] = pageSize;

            var executed = await next.Invoke();

            // FIXME: add fallback and warning

        }
    }
}