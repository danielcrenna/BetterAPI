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

        public TopActionFilter(IStringLocalizer<TopActionFilter> localizer, IOptionsSnapshot<TopOptions> options, ILogger<TopActionFilter> logger) : 
            base(localizer, options, logger)
        {
            _options = options;
        }
        
        public override async Task OnValidRequestAsync(Type underlyingType, StringValues clauses, ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executed = await next.Invoke();

        }
    }
}