// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BetterAPI
{
    /// <summary>
    /// Provides group-level (i.e. controller-level by default) documentation based on DisplayAttribute.
    /// </summary>
    internal sealed class DocumentationDocumentFilter : IDocumentFilter
    {
        private readonly IOptionsMonitor<ApiOptions> _options;
        private readonly ILogger<DocumentationDocumentFilter> _logger;
        private readonly HashSet<string> _removed;

        public DocumentationDocumentFilter(IOptionsMonitor<ApiOptions> options, ILogger<DocumentationDocumentFilter> logger)
        {
            _options = options;
            _logger = logger;
            _removed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            DocumentControllerSummaries(swaggerDoc, context);

            MaybeRemoveUnversionedRoutes(swaggerDoc, context);
        }

        private void MaybeRemoveUnversionedRoutes(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            if (_options.CurrentValue.Versioning.UseUrl)
            {
                //
                // Remove un-versioned routes to OpenAPI if we're using URL versioning:
                // The routes may still be callable if configuration allows, but clutter up the documentation.
                foreach (var apiDescription in context.ApiDescriptions)
                {
                    if (apiDescription.RelativePath.StartsWith("v"))
                        continue;

                    if (!swaggerDoc.Paths.Remove($"/{apiDescription.RelativePath}") && !_removed.Contains(apiDescription.RelativePath))
                        _logger.LogWarning($"Tried to remove path /{apiDescription.RelativePath}, but it wasn't there.");
                    else
                        _removed.Add(apiDescription.RelativePath);
                }
            }
        }

        private static void DocumentControllerSummaries(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            foreach (var description in context.ApiDescriptions)
            {
                if (!(description.ActionDescriptor is ControllerActionDescriptor descriptor))
                    continue;

                var controllerType = descriptor.ControllerTypeInfo;
                if (!controllerType.TryGetAttribute<DisplayAttribute>(true, out var display))
                    continue;

                var tag = new OpenApiTag {Name = display.GetGroupName() ?? descriptor.ControllerName};

                var summary = display.GetDescription();
                if (summary != null)
                {
                    tag.Description = summary;
                }

                if (!swaggerDoc.Tags.Any(x => x.Name.Equals(tag.Name, StringComparison.OrdinalIgnoreCase)))
                    swaggerDoc.Tags.Add(tag);
            }
        }
    }
}