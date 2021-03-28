using System.Threading.Tasks;
using BetterAPI.Extensions;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BetterAPI.Enveloping
{
    public sealed class EnvelopeActionFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executed = await next();

            if (executed.Result is ObjectResult result)
            {
                if (context.ActionDescriptor.ReturnsEnumerableType(out var collectionType))
                {
                    var body = executed.GetResultBody(result, out var settable);
                    if (settable)
                    {
                        var type = typeof(Envelope<>).MakeGenericType(collectionType!);
                        var envelope = Instancing.CreateInstance(type, body);
                        result.Value = envelope;
                    }
                }
            }
        }
    }
}
