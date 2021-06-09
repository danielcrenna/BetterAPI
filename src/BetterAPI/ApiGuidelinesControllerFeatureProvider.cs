// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BetterAPI.ChangeLog;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace BetterAPI
{
    internal sealed class ApiGuidelinesControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private readonly ChangeLogBuilder _builder;

        public ApiGuidelinesControllerFeatureProvider(ChangeLogBuilder builder)
        {
            _builder = builder;
        }

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            AddResourceControllers(parts, feature);
        }

        private void AddResourceControllers(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            var resourceTypes = _builder.ResourceTypes ?? new HashSet<Type>(0);
            
            foreach (var part in parts.OfType<AssemblyPart>())
            {
                if (part.Assembly == typeof(ApiGuidelinesControllerFeatureProvider).Assembly)
                    continue;

                AddUserDefinedResourceController(feature, part, resourceTypes);
            }
        }

        private static void AddUserDefinedResourceController(ControllerFeature feature, AssemblyPart part, ISet<Type> resourceTypes)
        {
            foreach (var type in part.Assembly.GetTypes())
            {
                if (type.HasAttribute<InternalResourceAttribute>())
                    continue; // prevent user accidentally surfacing internal resources in their changelog (minor potential exploit)

                if (!typeof(IResource).IsAssignableFrom(type))
                    continue; // not a resource

                if (!resourceTypes.Contains(type))
                    continue; // not in the change log

                var hasController = false;

                foreach (var controller in feature.Controllers)
                {
                    var controllerType = controller.AsType();
                    if (!controllerType.ImplementsGeneric(typeof(ResourceController<>)))
                        continue; // not a resource controller

                    if (!controllerType.IsGenericType)
                        continue; // an internal resource controller with a closed type

                    var resourceType = controllerType.GetGenericArguments()[0];
                    if (resourceType != type)
                        continue;
                    hasController = true;
                    break;
                }

                if (!hasController)
                {
                    feature.Controllers.Add(typeof(ResourceController<>).MakeGenericType(type).GetTypeInfo());
                }
            }
        }
    }
}