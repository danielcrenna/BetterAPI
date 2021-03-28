using System;
using System.Threading.Tasks;
using BetterAPI.DeltaQueries;
using BetterAPI.Extensions;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace BetterAPI.Deltas
{
    public sealed class DeltaQueryActionFilter : IAsyncActionFilter
    {
        private readonly IOptionsSnapshot<DeltaQueryOptions> _options;

        public DeltaQueryActionFilter(IOptionsSnapshot<DeltaQueryOptions> options)
        {
            _options = options;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!IsValidForRequest(context, out var collectionType) || collectionType == null)
            {
                await next.Invoke();
                return;
            }

            var executed = await next.Invoke();

            // FIXME:
            // "Note: If the collection is paginated the deltaLink will only be present on the final page but MUST reflect any changes to the data returned across all pages."

            if (executed.Result is ObjectResult result)
            {
                var body = executed.GetResultBody(result, out var settable);

                if (settable)
                {
                    // FIXME: append "@deltaLink": "{opaqueUrl}" to serialized body

                }
            }
        }

        internal bool IsValidForRequest(ActionContext context, out Type? collectionType)
        {
            if (context.HttpContext.Request.Method != HttpMethods.Get)
            {
                collectionType = null;
                return false;
            }

            if (context.HttpContext.Request.Query.TryGetValue(_options.Value.Operator, out _))
                return context.ActionDescriptor.ReturnsEnumerableType(out collectionType) || collectionType == null;

            collectionType = null;
            return false;
        }
    }
}
