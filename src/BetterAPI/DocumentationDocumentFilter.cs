// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BetterAPI
{
    /// <summary>
    /// Provides group-level (i.e. controller-level by default) documentation based on DisplayAttribute.
    /// </summary>
    internal sealed class DocumentationDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            foreach (var description in context.ApiDescriptions)
            {
                if (!(description.ActionDescriptor is ControllerActionDescriptor descriptor))
                    continue;

                var controllerType = descriptor.ControllerTypeInfo;
                if (!controllerType.TryGetAttribute<DisplayAttribute>(true, out var display))
                    continue;

                var tag = new OpenApiTag { Name = display.GetGroupName() ?? descriptor.ControllerName};

                var summary = display.GetDescription();
                if (summary != null)
                {
                    tag.Description = summary;
                }

                if(!swaggerDoc.Tags.Any(x => x.Name.Equals(tag.Name, StringComparison.OrdinalIgnoreCase)))
                    swaggerDoc.Tags.Add(tag);
            }
        }
    }
}