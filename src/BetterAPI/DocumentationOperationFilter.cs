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
using BetterAPI.Caching;
using BetterAPI.Data;
using BetterAPI.Extensions;
using BetterAPI.Reflection;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
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
        private readonly IResourceDataService _service;
        private readonly IOptionsMonitor<ApiOptions> _options;
        private readonly ILogger<DocumentationOperationFilter> _logger;

        public DocumentationOperationFilter(TypeRegistry registry, 
            IStringLocalizer<DocumentationOperationFilter> localizer, 
            IResourceDataService service,
            IOptionsMonitor<ApiOptions> options, 
            ILogger<DocumentationOperationFilter> logger)
        {
            _registry = registry;
            _localizer = localizer;
            _service = service;
            _options = options;
            _logger = logger;
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            EnsureOperationsHaveIds(operation, context);
            DocumentFeatures(operation, context);
            DocumentActions(operation, context);
            DocumentResponses(operation);
            DocumentParameters(operation);
            DocumentSchemas(context);
        }

        private void DocumentParameters(OpenApiOperation operation)
        {
            foreach (var parameter in operation.Parameters)
            {
                // FIXME:
                // There doesn't seem to be a way to SwaggerUI to have both a description/example, and a default value,
                // or a way to customize the Example value where it is only used as the UI's placeholder and doesn't
                // populate the field when "Try it out" is clicked:
                // https://github.com/swagger-api/swagger-ui/issues/5776

                if (parameter.Name.Equals("format", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Example = new OpenApiString("json");
                    parameter.Description = _localizer.GetString("The media type format expected by the client.");
                }

                if (parameter.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                {
                    if(operation.OperationId.StartsWith(Constants.Get))
                        parameter.Description = _localizer.GetString("The ID of the resource to retrieve.");

                    if(operation.OperationId.StartsWith(Constants.Update))
                        parameter.Description = _localizer.GetString("The ID of the resource to update.");

                    if(operation.OperationId.StartsWith(Constants.Delete))
                        parameter.Description = _localizer.GetString("The ID of the resource to delete.");
                }
            }
        }

        private void DocumentFeatures(OpenApiOperation operation, OperationFilterContext context)
        {
            DocumentPrefer(operation);
            DocumentHttpCaching(operation, context);
            DocumentLinks(operation);
            DocumentDeltaQueries(operation, context);

            if(_service.SupportsFiltering)
                DocumentFiltering(operation, context);

            DocumentPaging(operation, context);

            if(_service.SupportsSorting)
                DocumentSorting(operation, context);

            if(_service.SupportsShaping)
                DocumentShaping(operation);

            if(_service.SupportsSearch)
                DocumentSearch(operation);
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
                if (statusCode == Constants.Status200OkString)
                {
                    if (response.Description == null || response.Description == _localizer.GetString("Success"))
                    {
                        response.Description = _localizer.GetString("The operation succeeded, and there is additional content returned in the response.");
                    }
                }

                if (statusCode == Constants.Status201CreatedString)
                {
                    if (response.Description == null || response.Description == _localizer.GetString("Success"))
                    {
                        response.Description = _localizer.GetString("Returns the newly created resource, or an empty body if a minimal return is preferred.");
                    }
                }

                if (statusCode == Constants.Status204NoContentString)
                {
                    if (response.Description == null || response.Description == _localizer.GetString("Success"))
                    {
                        response.Description = _localizer.GetString("The operation succeeded, and there is no additional content available.");
                    }
                }

                if (statusCode == Constants.Status303SeeOtherString)
                {
                    if (response.Description == null || response.Description == _localizer.GetString("See Other"))
                    {
                        response.Description = _localizer.GetString("This resource already exists. Did you mean to update it?");
                    }
                }

                if (statusCode == Constants.Status304NotModifiedString)
                {
                    if (response.Description == null || response.Description == _localizer.GetString("Not Modified"))
                    {
                        response.Description = _localizer.GetString("The resource was not returned, because it was not modified according to the ETag or LastModifiedDate.");
                    }
                }

                if (statusCode == Constants.Status400BadRequestString)
                {
                    ProblemDetailsMediaType(operation, response);

                    if (response.Description == null || response.Description == _localizer.GetString("Bad Request"))
                    {
                        response.Description = _localizer.GetString("There was an error with the request, and further problem details are available.");
                    }
                }

                if (statusCode == Constants.Status404NotFoundString)
                {
                    ProblemDetailsMediaType(operation, response);

                    if (response.Description == null || response.Description == _localizer.GetString("Not Found"))
                    {
                        response.Description = _localizer.GetString("There is no resource with the specified ID on this server.");
                    }
                }

                if (statusCode == Constants.Status410GoneString)
                {
                    ProblemDetailsMediaType(operation, response);

                    if (response.Description == null || response.Description == _localizer.GetString("Client Error"))
                    {
                        response.Description = _localizer.GetString("The resource was already deleted from the server.");
                    }
                }

                if (statusCode == Constants.Status412PreconditionFailedString)
                {
                    ProblemDetailsMediaType(operation, response);

                    if (response.Description == null || response.Description == _localizer.GetString("Client Error"))
                    {
                        if(operation.OperationId.StartsWith(Constants.Create))
                            response.Description = _localizer.GetString("The resource was not created, because it has unmet pre-conditions.");

                        if(operation.OperationId.StartsWith(Constants.Update))
                            response.Description = _localizer.GetString("The resource was not updated, because it has unmet pre-conditions.");

                        if(operation.OperationId.StartsWith(Constants.Delete))
                            response.Description = _localizer.GetString("The resource was not deleted, because it has unmet pre-conditions.");
                    }
                }

                if (statusCode == Constants.Status413PayloadTooLargeString)
                {
                    ProblemDetailsMediaType(operation, response);

                    if (response.Description == null || response.Description == _localizer.GetString("Client Error"))
                    {
                        response.Description = _localizer.GetString("Requested page size was larger than the server's maximum page size.");
                    }
                }

                if (statusCode == Constants.Status500InternalServerErrorString)
                {
                    ProblemDetailsMediaType(operation, response);

                    if (response.Description == null || response.Description == _localizer.GetString("Server Error"))
                    {
                        if(operation.OperationId.StartsWith(Constants.Create))
                            response.Description = _localizer.GetString("An unexpected error occurred saving this resource. An error was logged. Please try again later.");

                        if(operation.OperationId.StartsWith(Constants.Delete))
                            response.Description = _localizer.GetString("An unexpected error occurred deleting this resource. An error was logged. Please try again later.");
                    }
                }
            }
        }

        private void ProblemDetailsMediaType(OpenApiOperation operation, OpenApiResponse response)
        {
            // API versioning replaces media types with incorrect values for ProblemDetails
            var reference = response.Content.Values.First();
            response.Content.Clear();

            // See: https://tools.ietf.org/html/rfc7807#section-6.2
            if (_options.CurrentValue.ApiFormats.HasFlagFast(ApiSupportedMediaTypes.ApplicationJson))
                response.Content["application/problem+json"] = reference;

            if (_options.CurrentValue.ApiFormats.HasFlagFast(ApiSupportedMediaTypes.ApplicationXml))
                response.Content["application/problem+xml"] = reference;
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

        private void EnsureOperationsHaveIds(OpenApiOperation operation, OperationFilterContext context)
        {
            // Assign a unique operation ID if one wasn't set by the developer
            if (string.IsNullOrWhiteSpace(operation.OperationId))
                operation.OperationId = CreateOperationId(context);
        }

        private string CreateOperationId(OperationFilterContext context)
        {
            var method = context.MethodInfo;

            if (method.ReturnType == typeof(void))
                return method.Name;

            var operationId = Pooling.StringBuilderPool.Scoped(sb =>
            {
                if (method.Name.StartsWith(Constants.Get))
                    sb.Append(Constants.Get);
                else if (method.Name.StartsWith(Constants.Create))
                    sb.Append(Constants.Create);
                else if (method.Name.StartsWith(Constants.Update))
                    sb.Append(Constants.Update);
                else if (method.Name.StartsWith(Constants.Delete))
                    sb.Append(Constants.Delete);

                Type? type = default;
                var plural = false;

                foreach (var producesResponseType in context.ApiDescription.ActionDescriptor.FilterDescriptors
                    .Select(x => x.Filter)
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

                if (parameters.Any(x =>
                    x.Name != null && x.Name.Equals(nameof(IResource.Id), StringComparison.OrdinalIgnoreCase)))
                    sb.Append("ById");

                if (parameters.Any(x =>
                    x.Name != null && x.Name.Equals("continuationToken", StringComparison.OrdinalIgnoreCase)))
                    sb.Append("NextPage");
            });
            
            _logger.LogDebug("OperationId: " + operationId);
            return operationId;
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

            if (operation.OperationId.EndsWith("NextPage"))
                return; // cannot sort an opaque query

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = _options.CurrentValue.Sort.Operator,
                In = ParameterLocation.Query,
                Description = _localizer.GetString("Apply a property-level sort to the collection query"),
                Example = new OpenApiString("id asc")
            });
        }

        private void DocumentFiltering(OpenApiOperation operation, OperationFilterContext context)
        {
            if (!context.ApiDescription.ActionDescriptor.IsCollectionQuery(out _))
                return;

            if (operation.OperationId.EndsWith("NextPage"))
                return; // cannot filter an opaque query

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

            if (operation.OperationId.EndsWith("NextPage"))
                return; // cannot page an opaque query

            if (_service.SupportsMaxPageSize)
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = _options.CurrentValue.Paging.MaxPageSize.Operator,
                    In = ParameterLocation.Query,
                    Description = _localizer.GetString("Request server-driven paging with a specific page size"),
                    Example = new OpenApiString("")
                });
            }

            if (_service.SupportsSkip)
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = _options.CurrentValue.Paging.Skip.Operator,
                    In = ParameterLocation.Query,
                    Description = _localizer.GetString("Apply a client-directed offset into a collection."),
                    Example = new OpenApiString("")
                });
            }

            if (_service.SupportsTop)
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = _options.CurrentValue.Paging.Top.Operator,
                    In = ParameterLocation.Query,
                    Description = _localizer.GetString("Apply a client-directed request to specify the number of results to return."),
                    Example = new OpenApiString("")
                });
            }

            if (_service.SupportsCount)
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = _options.CurrentValue.Paging.Count.Operator,
                    In = ParameterLocation.Query,
                    Description = _localizer.GetString("Requests the server to include the count of items in the response"),
                    Example = new OpenApiString("true")
                });
            }
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

            if (operation.OperationId.EndsWith("NextPage"))
                return; // cannot shape an opaque query

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

        private void DocumentSearch(OpenApiOperation operation)
        {
            if (!IsQuery(operation))
                return;

            if (operation.OperationId.EndsWith("NextPage"))
                return; // cannot search an opaque query

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = _options.CurrentValue.Search.Operator,
                In = ParameterLocation.Query,
                Description = _localizer.GetString("Perform a full text search with the given search criteria"),
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