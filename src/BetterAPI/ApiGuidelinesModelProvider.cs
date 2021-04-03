// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Linq;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace BetterAPI
{
    /// <summary>
    /// Provides routing features per API Guidelines.
    /// </summary>
    internal sealed class ApiGuidelinesModelProvider : IApplicationModelProvider
    {
        public void OnProvidersExecuting(ApplicationModelProviderContext context)
        {
            foreach (var controller in context.Result.Controllers)
            {
                if (!IsApiController(controller))
                    continue;

                foreach (var action in controller.Actions)
                {
                    TryAddCollectionRoute(action);
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

        private static void TryAddCollectionRoute(ActionModel actionModel)
        {
            if (IsAttributeRouted(actionModel.Controller.Selectors) || IsAttributeRouted(actionModel.Selectors))
                return;

            // Collections must be un-abbreviated and pluralized:
            // https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#93-collection-url-patterns
            //
            var collectionName = actionModel.Controller.ControllerName.Pluralize();
            var route = new RouteAttribute(collectionName);
            var selector = new SelectorModel {AttributeRouteModel = new AttributeRouteModel(route)};
            actionModel.Controller.Selectors.Add(selector);
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