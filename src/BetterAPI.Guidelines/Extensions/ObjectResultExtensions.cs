using System;
using Microsoft.AspNetCore.Mvc;

namespace BetterAPI.Guidelines.Extensions
{
    internal static class ActionContextExtensions
    {
        public static object GetResultBody(this ActionContext context, ObjectResult result) => context.GetResultBody(result, out _);

        public static object GetResultBody(this ActionContext context, ObjectResult result, out bool settable)
        {
            var body = result.Value;
            if (body != default)
            {
                settable = true;
                return body;
            }

            body = context.HttpContext.Items[Constants.ObjectResultValue] ?? throw new NullReferenceException("Could not locate expected result body");
            settable = false;
            return body;
        }
    }
}
