using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace BetterAPI.Paging
{
    public sealed class SkipActionFilter : CollectionQueryActionFilter<SkipOptions>
    {
        private readonly IOptionsSnapshot<SkipOptions> _options;

        public SkipActionFilter(IStringLocalizer<SkipActionFilter> localizer, IOptionsSnapshot<SkipOptions> options, ILogger<SkipActionFilter> logger) : 
            base(localizer, options, logger)
        {
            _options = options;
        }
        
        public override async Task OnValidRequestAsync(Type underlyingType, StringValues clauses, ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (clauses.Count != 1 || !int.TryParse(clauses[0], out var skip))
            {
                await next.Invoke();
                return;
            }

            context.HttpContext.Items[Constants.SkipOperationContextKey] = skip;

            var executed = await next.Invoke();

            // FIXME: add fallback and warning
        }
    }
}