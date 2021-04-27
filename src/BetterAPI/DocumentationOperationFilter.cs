// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Mime;
using System.Text;
using BetterAPI.Caching;
using BetterAPI.Extensions;
using BetterAPI.Reflection;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Localization;
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
        private readonly TypeRegistry _registry;
        private readonly IStringLocalizer<DocumentationOperationFilter> _localizer;
        private readonly IOptionsMonitor<ApiOptions> _options;

        public DocumentationOperationFilter(TypeRegistry registry, IStringLocalizer<DocumentationOperationFilter> localizer, IOptionsMonitor<ApiOptions> options)
        {
            _registry = registry;
            _localizer = localizer;
            _options = options;
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            EnsureOperationsHaveIds(operation, context);
            DocumentFeatures(operation, context);
            DocumentActions(operation, context);
            DocumentResponses(operation);
            DocumentSchemas(context);
        }
        
        private void DocumentFeatures(OpenApiOperation operation, OperationFilterContext context)
        {
            DocumentPrefer(operation);
            DocumentHttpCaching(operation, context);
            DocumentLinks(operation);
            DocumentSorting(operation, context);
            DocumentFiltering(operation, context);
            DocumentPaging(operation, context);
            DocumentDeltaQueries(operation, context);
            DocumentShaping(operation);
        }

        private void DocumentActions(OpenApiOperation operation, OperationFilterContext context)
        {
            if (!(context.ApiDescription.ActionDescriptor is ControllerActionDescriptor descriptor))
                return;

            if (!descriptor.MethodInfo.TryGetAttribute<DisplayAttribute>(true, out var display))
                return;

            var description = display.GetDescription();
            if (!string.IsNullOrWhiteSpace(description))
                operation.Description = _localizer.GetString(description);

            var summary = display.GetName() ?? operation.Description;
            if (!string.IsNullOrWhiteSpace(summary))
                operation.Summary = _localizer.GetString(summary);

            var groupName = display.GetGroupName();

            if (!string.IsNullOrWhiteSpace(groupName))
            {
                var controllerNameTag = operation.Tags.SingleOrDefault(x =>
                    x.Name.Equals(descriptor.ControllerName, StringComparison.OrdinalIgnoreCase));

                if (controllerNameTag != default && operation.Tags.Remove(controllerNameTag))
                    operation.Tags.Add(new OpenApiTag { Name = _localizer.GetString(groupName) });
            }

            if (operation.RequestBody != null)
            {
                // NOTE: Swashbuckle is not respecting the ConsumesAttribute formats here, so we have to do it manually
                var content = operation.RequestBody.Content.First().Value;
                operation.RequestBody.Content.Clear();

                switch (_options.CurrentValue.ApiFormats)
                {
                    case ApiSupportedMediaTypes.None:
                        throw new NotSupportedException(_localizer.GetString("API must support at least one content format"));
                    case ApiSupportedMediaTypes.ApplicationJson | ApiSupportedMediaTypes.ApplicationXml:
                        operation.RequestBody.Content.Add(MediaTypeNames.Application.Json, new OpenApiMediaType { Schema = content.Schema });
                        operation.RequestBody.Content.Add(MediaTypeNames.Application.Xml, new OpenApiMediaType { Schema = content.Schema });
                        break;
                    case ApiSupportedMediaTypes.ApplicationJson:
                        operation.RequestBody.Content.Add(MediaTypeNames.Application.Json, new OpenApiMediaType { Schema = content.Schema });
                        break;
                    case ApiSupportedMediaTypes.ApplicationXml:
                        operation.RequestBody.Content.Add(MediaTypeNames.Application.Xml, new OpenApiMediaType { Schema = content.Schema });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var prompt = display.GetPrompt();
                if (!string.IsNullOrWhiteSpace(prompt))
                {
                    operation.RequestBody.Description = _localizer.GetString(prompt);
                }
            }
        }

        private void DocumentResponses(OpenApiOperation operation)
        {
            foreach (var (statusCode, response) in operation.Responses)
            {
                if (statusCode == StatusCodes.Status201Created.ToString())
                {
                    if (response.Description == null || response.Description == _localizer.GetString("Success"))
                    {
                        response.Description = _localizer.GetString("Returns the newly created resource, or an empty body if a minimal return is preferred");
                    }
                }

                if (statusCode == StatusCodes.Status304NotModified.ToString())
                {
                    if (response.Description == null || response.Description == _localizer.GetString("Not Modified"))
                    {
                        response.Description = _localizer.GetString("The resource was not returned, because it was not modified according to the ETag or LastModifiedDate.");
                    }
                }

                // FIXME: ProblemDetails should use application/problem+json as the media type.
                if (statusCode == StatusCodes.Status400BadRequest.ToString())
                {
                    if (response.Description == null || response.Description == _localizer.GetString("Bad Request"))
                    {
                        response.Description = _localizer.GetString("There was an error with the request, and further problem details are available");
                    }
                }

                // FIXME: ProblemDetails should use application/problem+json as the media type.
                if (statusCode == StatusCodes.Status412PreconditionFailed.ToString())
                {
                    if (response.Description == null || response.Description == _localizer.GetString("Client Error"))
                    {
                        response.Description = _localizer.GetString("The resource was not created, because it has unmet pre-conditions");
                    }
                }

                // FIXME: ProblemDetails should use application/problem+json as the media type.
                if (statusCode == StatusCodes.Status413PayloadTooLarge.ToString())
                {
                    if (response.Description == null || response.Description == _localizer.GetString("Client Error"))
                    {
                        response.Description = _localizer.GetString("Requested page size was larger than the server's maximum page size.");
                    }
                }
            }
        }

        private void DocumentSchemas(OperationFilterContext context)
        {
            // This will run multiple times, so we store previously discovered types
            foreach (var (typeName, schema) in context.SchemaRepository.Schemas)
            {
                if (!_registry.TryGetValue(typeName, out var type) || type == default)
                    continue;

                var members = AccessorMembers.Create(type, AccessorMemberTypes.Properties, AccessorMemberScope.Public);

                foreach (var (propertyName, property) in schema.Properties)
                {
                    if (!members.TryGetValue(propertyName, out var member))
                        continue;
                    if (!member.TryGetAttribute(out DisplayAttribute attribute))
                        continue;

                    var description = attribute.GetDescription();
                    if (!string.IsNullOrWhiteSpace(description))
                        property.Description = _localizer.GetString(description);

                    var prompt = attribute.GetPrompt();
                    if(!string.IsNullOrWhiteSpace(prompt))
                        property.Example = new OpenApiString(_localizer.GetString(prompt));
                }
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

            if (method.Name.StartsWith(Constants.Get))
                sb.Append(Constants.Get);
            if (method.Name.StartsWith(Constants.Create))
                sb.Append(Constants.Create);
            if (method.Name.StartsWith(Constants.Update))
                sb.Append(Constants.Update);
            if (method.Name.StartsWith(Constants.Delete))
                sb.Append(Constants.Delete);

            Type? type = default;
            var plural = false;

            foreach (var producesResponseType in context.MethodInfo.GetCustomAttributes(true)
                .OfType<ProducesResponseTypeAttribute>())
            {
                if (producesResponseType.Type == typeof(void))
                    continue;
                if (producesResponseType.StatusCode <= 199 || producesResponseType.StatusCode >= 299)
                    continue;

                type = GetModelType(producesResponseType.Type, out plural);
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
                        type = GetModelType(parameter.ParameterType, out plural);
                        break;
                    }
                }

            type ??= GetModelType(method.ReturnType, out plural);
            sb.Append(plural ? type.Name.Pluralize() : type.Name);

            if (parameters.Any(x => x.Name != null && x.Name.Equals(nameof(IResource.Id), StringComparison.OrdinalIgnoreCase)))
                sb.Append("ById");

            return sb.ToString();
        }

        private void DocumentPrefer(OpenApiOperation operation)
        {
            if (IsMutation(operation))
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = ApiHeaderNames.Prefer,
                    In = ParameterLocation.Header,
                    Description = _localizer.GetString("Apply a preference for the response (return=minimal|representation)"),
                    Example = new OpenApiString(Constants.Prefer.ReturnRepresentation)
                });
        }

        private void DocumentHttpCaching(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.ApiDescription.ActionDescriptor.EndpointMetadata.Any(x => x is DoNotHttpCacheAttribute))
                return; // explicit opt-out

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = ApiHeaderNames.IfNoneMatch,
                In = ParameterLocation.Header,
                Description = _localizer.GetString("Only supply a result or perform an action if it does not match the specified entity resource identifier (ETag)"),
                Example = new OpenApiString("")
            });

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = ApiHeaderNames.IfMatch,
                In = ParameterLocation.Header,
                Description = _localizer.GetString("Only supply a result or perform an action if it matches the specified entity resource identifier (ETag)"),
                Example = new OpenApiString("")
            });

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = ApiHeaderNames.IfModifiedSince,
                In = ParameterLocation.Header,
                Description = _localizer.GetString("Only supply a result or perform an action if the resource's logical timestamp has been modified since the given date (Last-Modified)"),
                Example = new OpenApiString("")
            });

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = ApiHeaderNames.IfUnmodifiedSince,
                In = ParameterLocation.Header,
                Description = _localizer.GetString("Only supply a result or perform an action if the resource's logical timestamp has not been modified since the given date (Last-Modified)"),
                Example = new OpenApiString("")
            });
        }

        private static bool IsQuery(OpenApiOperation operation)
        {
            return !IsMutation(operation);
        }


        private static bool IsMutation(OpenApiOperation operation)
        {
            return operation.OperationId.StartsWith(Constants.Create, StringComparison.OrdinalIgnoreCase) ||
                   operation.OperationId.StartsWith(Constants.Update, StringComparison.OrdinalIgnoreCase) ||
                   operation.OperationId.StartsWith(Constants.Delete, StringComparison.OrdinalIgnoreCase);
        }

        private void DocumentLinks(OpenApiOperation operation)
        {
            foreach (var response in operation.Responses.Where(
                response => response.Key == Constants.Status201CreatedString))
                AddGetByIdLink(operation, response);
        }

        private void AddGetByIdLink(OpenApiOperation operation, KeyValuePair<string, OpenApiResponse> response)
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

            var modelName = operation.OperationId.Replace(Constants.Create, string.Empty);
            var operationId = _localizer.GetString($"Get{modelName}ById");

            var getById = new OpenApiLink
            {
                OperationId = operationId,
                Description = _localizer.GetString($"The `id` value returned in the response can be used as the `id` parameter in `GET /{modelName}/{{id}}`"),
            };

            // parameters:
            //    id: '$response.body#/id'
            getById.Parameters.Add("id",
                new RuntimeExpressionAnyWrapper
                    {Expression = new ResponseExpression(new BodyExpression(new JsonPointer("/id")))});

            response.Value.Links.Add(operationId.Value, getById);
        }

        private void DocumentSorting(OpenApiOperation operation, OperationFilterContext context)
        {
            if (!context.ApiDescription.ActionDescriptor.IsCollectionQuery(out _))
                return;

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = _options.CurrentValue.Sort.Operator,
                In = ParameterLocation.Query,
                Description = _localizer.GetString("Apply a property-level sort to the collection query"),
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
                Description = _localizer.GetString("Apply a property-level filter to the collection query"),
                Example = new OpenApiString("")
            });
        }

        private void DocumentPaging(OpenApiOperation operation, OperationFilterContext context)
        {
            if (!context.ApiDescription.ActionDescriptor.IsCollectionQuery(out _))
                return;

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = _options.CurrentValue.Paging.MaxPageSize.Operator,
                In = ParameterLocation.Query,
                Description = _localizer.GetString("Request server-driven paging with a specific page size"),
                Example = new OpenApiString("")
            });

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = _options.CurrentValue.Paging.Skip.Operator,
                In = ParameterLocation.Query,
                Description = _localizer.GetString("Apply a client-directed offset into a collection."),
                Example = new OpenApiString("")
            });

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = _options.CurrentValue.Paging.Top.Operator,
                In = ParameterLocation.Query,
                Description = _localizer.GetString("Apply a client-directed request to specify the number of results to return."),
                Example = new OpenApiString("")
            });

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = _options.CurrentValue.Paging.Count.Operator,
                In = ParameterLocation.Query,
                Description = _localizer.GetString("Requests the server to include the count of items in the response"),
                Example = new OpenApiString("true")
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
                Description = _localizer.GetString("Add an opaque URL to the end of the collection results for querying deltas since the query was executed."),
                Example = new OpenApiString(_options.CurrentValue.DeltaQueries.Operator)
            });
        }

        private void DocumentShaping(OpenApiOperation operation)
        {
            if (!IsQuery(operation))
                return;

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = _options.CurrentValue.Include.Operator,
                In = ParameterLocation.Query,
                Description = _localizer.GetString("Only include the specified fields in the response body"),
                Example = new OpenApiString("")
            });

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = _options.CurrentValue.Exclude.Operator,
                In = ParameterLocation.Query,
                Description = _localizer.GetString("Omit the specified fields in the response body"),
                Example = new OpenApiString("")
            });
        }

        private static Type GetModelType(Type type, out bool plural)
        {
            if (!type.IsGenericType)
            {
                plural = false;
                return type;
            }

            var definition = type.GetGenericTypeDefinition();

            if (definition == typeof(IEnumerable<>))
            {
                plural = true;
                return type.GetGenericArguments()[0];
            }

            foreach (var @interface in definition.GetInterfaces())
                if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    plural = true;
                    return type.GetGenericArguments()[0];
                }

            plural = false;
            return type;
        }
    }
}