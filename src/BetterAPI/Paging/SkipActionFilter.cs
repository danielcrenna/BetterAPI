using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace BetterAPI.Paging
{
    public sealed class SkipActionFilter : CollectionQueryActionFilter<SkipOptions>
    {
        private readonly IOptionsSnapshot<SkipOptions> _options;

        public SkipActionFilter(IOptionsSnapshot<SkipOptions> options, ILogger<SkipActionFilter> logger) : 
            base(options, logger)
        {
            _options = options;
        }
        
        public override async Task OnValidRequestAsync(Type underlyingType, StringValues clauses, ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executed = await next.Invoke();

        }
    }
}