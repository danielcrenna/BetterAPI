// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace BetterAPI
{
    /*
      [Display(Description = "Manages operations for weather forecast resources")]
      public class WeatherForecastController : ResourceController<WeatherForecast>
      {
          public WeatherForecastController(WeatherForecastService service, IEventBroadcaster events, IOptionsSnapshot<ApiOptions> options, ILogger<WeatherForecastController> logger) : 
              base(service, events, options, logger) { }
      }
     */

    internal sealed class ApiGuidelinesControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            AddResourceControllers(parts, feature);
        }

        private static void AddResourceControllers(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            foreach (var part in parts.OfType<AssemblyPart>())
            {
                if (part.Assembly == typeof(ApiGuidelinesControllerFeatureProvider).Assembly)
                    continue;

                foreach (var type in part.Assembly.GetTypes())
                {
                    if (!typeof(IResource).IsAssignableFrom(type))
                        continue;

                    var hasController = false;

                    foreach (var controller in feature.Controllers)
                    {
                        var controllerType = controller.AsType();
                        if (!controllerType.ImplementsGeneric(typeof(ResourceController<>)))
                            continue;

                        controllerType = controllerType.BaseType;
                        if (controllerType == default)
                            continue;

                        var resourceType = controllerType.GetGenericArguments()[0];
                        if (resourceType != type)
                            continue;
                        hasController = true;
                        break;
                    }

                    if (!hasController)
                        feature.Controllers.Add(typeof(ResourceController<>).MakeGenericType(type).GetTypeInfo());
                }
            }
        }
    }
}