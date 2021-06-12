// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace BetterAPI
{
    /// <summary>
    /// Responsible for dynamic routing, i.e. localization, policy access, feature management, etc. 
    /// </summary>
    internal sealed class ApiRouter : DynamicRouteValueTransformer
    {
        private readonly ResourceTypeRegistry _registry;

        public ApiRouter(ResourceTypeRegistry registry)
        {
            _registry = registry;
        }

        public override ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
        {
            const string routeKey = "route";

            if (!values.TryGetValue(routeKey, out var route) || !(route is string routeValue) ||
                string.IsNullOrWhiteSpace(routeValue))
                return new ValueTask<RouteValueDictionary>(values);

            // Convention:
            // - any discovered types in type registry are likely to be resources
            // - resources must have a corresponding controller with the name {resourceName}Controller
            // - we can map the pluralized route value to the singular controller name

            // See: https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#93-collection-url-patterns

            var resourceName = routeValue.Singularize();
            if (_registry.GetOrRegisterByName(resourceName, out _))
            {
                values["controller"] = resourceName;
            }

            return new ValueTask<RouteValueDictionary>(values);
        }
    }
}