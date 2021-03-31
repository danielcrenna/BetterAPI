// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using BetterAPI.Caching;
using BetterAPI.Extensions;
using BetterAPI.Reflection;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Expressions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BetterAPI
{
    /// <summary>
    ///     Provides extended OpenAPI documentation and annotations for middleware-provided features.
    /// </summary>
    internal sealed class DocumentationOperationFilter : IOperationFilter
    {
        // FIXME: We need to rebuild the swagger document if the options change
        private readonly IOptionsMonitor<ApiOptions> _options;

        public DocumentationOperationFilter(IOptionsMonitor<ApiOptions> options)
        {
            _options = options;
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            EnsureOperationsHaveIds(operation, context);

            DocumentFeatures(operation, context);

            DocumentActions(operation, context);
        }

        private void DocumentFeatures(OpenApiOperation operation, OperationFilterContext context)
        {
            DocumentPrefer(operation);
            DocumentHttpCaching(operation, context);
            DocumentLinks(operation);
            DocumentSorting(operation, context);
            DocumentFiltering(operation, context);
            DocumentDeltaQueries(operation, context);
        }

        private static void DocumentActions(OpenApiOperation operation, OperationFilterContext context)
        {
            if (!(context.ApiDescription.ActionDescriptor is ControllerActionDescriptor descriptor))
                return;

            if (!descriptor.MethodInfo.TryGetAttribute<DisplayAttribute>(true, out var display))
                return;

            var summary = display.GetName();
            if(summary != default)
                operation.Summary = display.GetName();

            var description = display.GetDescription();
            if(description != default)
                operation.Description = display.GetDescription();

            var groupName = display.GetGroupName();
            if (groupName != default)
            {
                var controllerNameTag = operation.Tags.SingleOrDefault(x =>
                    x.Name.Equals(descriptor.ControllerName, StringComparison.OrdinalIgnoreCase));

                if (controllerNameTag != default && operation.Tags.Remove(controllerNameTag))
                    operation.Tags.Add(new OpenApiTag { Name = groupName });
            }
        }

        private static void EnsureOperationsHaveIds(OpenApiOperation operation, OperationFilterContext context)
        {
            // Assign a unique operation ID if one wasn't set by the developer
            if (string.IsNullOrWhiteSpace(operation.OperationId))
                operation.OperationId = CreateOperationId(context);
        }

        private static string CreateOperationId(OperationFilterContext context)
        {
            var method = context.MethodInfo;

            if (method.ReturnType == typeof(void))
                return method.Name;

            var sb = new StringBuilder();

            if (method.Name.StartsWith("Get"))
                sb.Append("Get");
            if (method.Name.StartsWith("Create"))
                sb.Append("Create");
            if (method.Name.StartsWith("Update"))
                sb.Append("Update");
            if (method.Name.StartsWith("Delete"))
                sb.Append("Delete");

            Type? type = default;
            var plural = false;

            foreach (var producesResponseType in context.MethodInfo.GetCustomAttributes(true)
                .OfType<ProducesResponseTypeAttribute>())
            {
                if (producesResponseType.Type == typeof(void))
                    continue;
                if (producesResponseType.StatusCode <= 199 || producesResponseType.StatusCode >= 299)
                    continue;

                type = producesResponseType.Type.GetModelType(out plural);
                break;
            }

            var parameters = method.GetParameters();

            if (type == default)
                foreach (var parameter in parameters)
                {
                    var attributes = parameter.GetCustomAttributes(true);

                    foreach (var attribute in attributes)
                    {
                        if (!(attribute is FromBodyAttribute))
                            continue;
                        type = parameter.ParameterType.GetModelType(out plural);
                        break;
                    }
                }

            type ??= method.ReturnType.GetModelType(out plural);
            sb.Append(plural ? type.Name.Pluralize() : type.Name);

            if (parameters.Any(x => x.Name != null && x.Name.Equals("id", StringComparison.OrdinalIgnoreCase)))
                sb.Append("ById");

            return sb.ToString();
        }

        private static void DocumentPrefer(OpenApiOperation operation)
        {
            if (IsMutation(operation))
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = ApiHeaderNames.Prefer,
                    In = ParameterLocation.Header,
                    Description = "Apply a preference for the response (return=minimal|representation)",
                    Example = new OpenApiString(Constants.Prefer.ReturnRepresentation)
                });
        }

        private static void DocumentHttpCaching(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.ApiDescription.ActionDescriptor.EndpointMetadata.Any(x => x is DoNotHttpCacheAttribute))
                return; // explicit opt-out

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "If-None-Match",
                In = ParameterLocation.Header,
                Description = "Only supply a result or perform an action if it does not match the specified entity resource identifier (ETag)",
                Example = new OpenApiString("")
            });

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "If-Match",
                In = ParameterLocation.Header,
                Description = "Only supply a result or perform an action if it matches the specified entity resource identifier (ETag)",
                Example = new OpenApiString("")
            });

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "If-Modified-Since",
                In = ParameterLocation.Header,
                Description =
                    "Only supply a result or perform an action if the resource's logical timestamp has been modified since the given date (Last-Modified)",
                Example = new OpenApiString("")
            });

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "If-Unmodified-Since",
                In = ParameterLocation.Header,
                Description = "Only supply a result or perform an action if the resource's logical timestamp has not been modified since the given date (Last-Modified)",
                Example = new OpenApiString("")
            });
        }

        private static bool IsMutation(OpenApiOperation operation)
        {
            return operation.OperationId.StartsWith("Create", StringComparison.OrdinalIgnoreCase) ||
                   operation.OperationId.StartsWith("Update", StringComparison.OrdinalIgnoreCase) ||
                   operation.OperationId.StartsWith("Delete", StringComparison.OrdinalIgnoreCase);
        }

        private static void DocumentLinks(OpenApiOperation operation)
        {
            foreach (var response in operation.Responses.Where(
                response => response.Key == Constants.Status201CreatedString))
                AddGetByIdLink(operation, response);
        }

        private static void AddGetByIdLink(OpenApiOperation operation, KeyValuePair<string, OpenApiResponse> response)
        {
            /*
                # See: https://swagger.io/docs/specification/links/
                #   
                # -----------------------------------------------------
                # Links
                # -----------------------------------------------------
                links:
                  GetUserByUserId:   # <---- arbitrary name for the link
                    operationId: getUser
                    # or
                    # operationRef: '#/paths/~1users~1{userId}/get'
                    parameters:
                      userId: '$response.body#/id'
                    description: >
                      The `id` value returned in the response can be used as the `userId` parameter in `GET /users/{userId}`.
                # -----------------------------------------------------
             */

            var modelName = operation.OperationId.Replace("Create", string.Empty);
            var operationId = $"Get{modelName}ById";

            var getById = new OpenApiLink
            {
                OperationId = operationId,
                Description =
                    $"The `id` value returned in the response can be used as the `id` parameter in `GET /{modelName}/{{id}}`",
            };

            // parameters:
            //    id: '$response.body#/id'
            getById.Parameters.Add("id",
                new RuntimeExpressionAnyWrapper
                    {Expression = new ResponseExpression(new BodyExpression(new JsonPointer("/id")))});

            response.Value.Links.Add(operationId, getById);
        }

        private void DocumentSorting(OpenApiOperation operation, OperationFilterContext context)
        {
            if (!context.ApiDescription.ActionDescriptor.IsCollectionQuery(out _))
                return;

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = _options.CurrentValue.Sort.Operator,
                In = ParameterLocation.Query,
                Description = "Apply a property-level sort to the collection query",
                Example = new OpenApiString($"{_options.CurrentValue.Sort.Operator}=id asc")
            });
        }

        private void DocumentFiltering(OpenApiOperation operation, OperationFilterContext context)
        {
            if (!context.ApiDescription.ActionDescriptor.IsCollectionQuery(out _))
                return;

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = _options.CurrentValue.Filter.Operator,
                In = ParameterLocation.Query,
                Description = "Apply a property-level filter to the collection query",
                Example = new OpenApiString("")
            });
        }

        private void DocumentDeltaQueries(OpenApiOperation operation, OperationFilterContext context)
        {
            if (!context.ApiDescription.ActionDescriptor.IsCollectionQuery(out _))
                return;

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = _options.CurrentValue.DeltaQueries.Operator,
                In = ParameterLocation.Query,
                Description = "Add an opaque URL to the end of the collection results for querying deltas since the query was executed.",
                Example = new OpenApiString(_options.CurrentValue.DeltaQueries.Operator)
            });
        }
    }
}