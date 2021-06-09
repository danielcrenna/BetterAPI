// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Linq;
using BetterAPI.ChangeLog;
using BetterAPI.Extensions;
using BetterAPI.Http;
using Humanizer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;

namespace BetterAPI
{
    /// <summary>
    /// Provides routing features per API Guidelines.
    /// </summary>
    internal sealed class ApiGuidelinesModelProvider : IApplicationModelProvider
    {
        private readonly ChangeLogBuilder _builder;
        private readonly IOptionsMonitor<ApiOptions> _options;
        private readonly IOptionsMonitor<RequestLocalizationOptions> _localization;

        public ApiGuidelinesModelProvider(ChangeLogBuilder builder, IOptionsMonitor<ApiOptions> options, IOptionsMonitor<RequestLocalizationOptions> localization)
        {
            _builder = builder;
            _options = options;
            _localization = localization;
        }

        public void OnProvidersExecuting(ApplicationModelProviderContext context)
        {
            foreach (var controller in context.Result.Controllers)
            {
                var isInternal = IsInternalController(controller);

                if (!IsApiController(controller) && !isInternal)
                    continue;

                foreach (var action in controller.Actions)
                {
                    TryAddCollectionRoute(action, isInternal);
                }
            }
        }

        public void OnProvidersExecuted(ApplicationModelProviderContext context) { }

        /// <remarks>
        /// Order is set to execute after the <see cref="DefaultApplicationModelProvider"/> but before <see cref="ApiBehaviorApplicationModelProvider" />,
        /// because the latter will throw an exception if any controller doesn't have a route attribute, but we're adding them by convention here.
        /// </remarks>
        public int Order => -1000 + 100 - 1;

        private static bool IsApiController(ControllerModel controller)
        {
            if (controller.Attributes.OfType<IApiBehaviorMetadata>().Any())
                return true;

            var controllerAssembly = controller.ControllerType.Assembly;
            var assemblyAttributes = controllerAssembly.GetCustomAttributes(false);
            return assemblyAttributes.OfType<IApiBehaviorMetadata>().Any();
        }

        private static bool IsInternalController(ICommonModel controller)
        {
            return controller.Attributes.OfType<InternalControllerAttribute>().Any();
        }

        private void TryAddCollectionRoute(ActionModel actionModel, bool isInternal)
        {
            if (IsAttributeRouted(actionModel.Controller.Selectors) || IsAttributeRouted(actionModel.Selectors))
                return; // don't change a route if one is explicitly provided on the controller or its action

            // Collections must be un-abbreviated and pluralized:
            // https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#93-collection-url-patterns
            //
            var resourceName = actionModel.Controller.ControllerType.NormalizeResourceControllerName();

            if (actionModel.Controller.ControllerType.IsGenericType)
            {
                var resourceType = actionModel.Controller.ControllerType.GetGenericArguments()[0];
                if (_builder.TryGetResourceNameForType(resourceType, out var name))
                {
                    resourceName = name;
                }
            }

            var collectionName = resourceName.Pluralize();

            AddCollectionRoutes(actionModel, isInternal, collectionName);
        }

        private void AddCollectionRoutes(ActionModel actionModel, bool isInternal, string collectionName)
        {
            if (isInternal)
            {
                if (_localization.CurrentValue.SupportedCultures.Count > 1)
                {
                    // [Route("api/{culture}/v{version:apiVersion}/{resourceNamePlural}"]
                    var template = $"api/{{culture}}/{collectionName}";
                    var route = new RouteAttribute(template);
                    var selector = new SelectorModel {AttributeRouteModel = new AttributeRouteModel(route)};
                    actionModel.Controller.Selectors.Add(selector);
                }

                {
                    // [Route("api/{resourceNamePlural}"]
                    var template = $"api/{collectionName}";
                    var route = new RouteAttribute(template);
                    var selector = new SelectorModel {AttributeRouteModel = new AttributeRouteModel(route)};
                    actionModel.Controller.Selectors.Add(selector);
                }
            }
            else
            {
                if (_options.CurrentValue.Versioning.UseUrl)
                {
                    if (_localization.CurrentValue.SupportedCultures.Count > 1)
                    {
                        // [Route("{culture}/v{version:apiVersion}/{resourceNamePlural}"]
                        var template = $"{{culture}}/v{{version:apiVersion}}/{collectionName}";
                        var route = new RouteAttribute(template);
                        var selector = new SelectorModel {AttributeRouteModel = new AttributeRouteModel(route)};
                        actionModel.Controller.Selectors.Add(selector);
                    }

                    {
                        // [Route("v{version:apiVersion}/{resourceNamePlural}"]
                        var template = $"v{{version:apiVersion}}/{collectionName}";
                        var route = new RouteAttribute(template);
                        var selector = new SelectorModel {AttributeRouteModel = new AttributeRouteModel(route)};
                        actionModel.Controller.Selectors.Add(selector);
                    }
                }

                {
                    // [Route("{resourceNamePlural}")]
                    var route = new RouteAttribute(collectionName);
                    var selector = new SelectorModel {AttributeRouteModel = new AttributeRouteModel(route)};
                    actionModel.Controller.Selectors.Add(selector);
                }
            }
        }

        private static bool IsAttributeRouted(IEnumerable<SelectorModel> selectorModel)
        {
            foreach (var selector in selectorModel)
            {
                if (selector.AttributeRouteModel != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}