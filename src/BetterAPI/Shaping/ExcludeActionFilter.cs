// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BetterAPI.Extensions;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace BetterAPI.Shaping
{
    public sealed class ExcludeActionFilter : QueryActionFilter<ExcludeOptions>
    {
        private readonly IStringLocalizer<ExcludeActionFilter> _localizer;
        private readonly ILogger<ExcludeActionFilter> _logger;

        public ExcludeActionFilter(IStringLocalizer<ExcludeActionFilter> localizer, IOptionsSnapshot<ExcludeOptions> options, ILogger<ExcludeActionFilter> logger) : base(options, logger)
        {
            _localizer = localizer;
            _logger = logger;
        }

        public override async Task OnValidRequestAsync(Type underlyingType, StringValues clauses, ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var members = AccessorMembers.Create(underlyingType, AccessorMemberTypes.Properties, AccessorMemberScope.Public);

            var exclusions = new List<string>();
            foreach (var value in clauses)
            {
                var fields = value.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (fields.Length == 0)
                    continue; // (FIXME: add a validation error?)

                foreach (var field in fields)
                {
                    if (!members.TryGetValue(field, out var member) || exclusions.Contains(member.Name))
                        continue;

                    exclusions.Add(member.Name);
                }
            }

            var inclusions = new List<string>();
            foreach (var member in members)
            {
                if(!exclusions.Contains(member.Name))
                    inclusions.Add(member.Name);
            }

            if (inclusions.Count == 0)
            {
                await next.Invoke();
                return;
            }

            context.HttpContext.Items.TryAdd(Constants.IncludeContextKey, inclusions);

            var executed = await next.Invoke();

            if (executed.HttpContext.Items.ContainsKey(Constants.IncludeContextKey))
            {
                executed.HttpContext.Items.Remove(Constants.IncludeContextKey);

                _logger.LogWarning(_localizer.GetString("Shaping operation has fallen back to object-level shaping. " +
                                                        "This means that shaping was not performed by the underlying data store, and is likely " +
                                                        "performing excessive work to return discarded values."));
            }
            
            if (executed.Result is ObjectResult result && !(result.Value is ProblemDetails))
            {
                var body = executed.GetResultBody(result, out var settable);

                if (settable)
                {
                    if (exclusions.Count > 0)
                    {
                        // FIXME: Instancing.CreateInstance will crash on ShapedData<> and Envelope<>,
                        //        so we have to use manual reflection until that is resolved
                        // var delta = Instancing.CreateInstance(typeof(DeltaAnnotated<>).MakeGenericType(deltaType), body, deltaLink);    
                        var shapingType = typeof(ShapedData<>).MakeGenericType(underlyingType);
                        var shaped = Activator.CreateInstance(shapingType, body, inclusions);
                        result.Value = shaped;
                    }
                }
            }
        }
    }
}