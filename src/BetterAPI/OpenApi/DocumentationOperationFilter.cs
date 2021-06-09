// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BetterAPI.Caching;
using BetterAPI.Data;
using BetterAPI.Extensions;
using BetterAPI.Patch;
using BetterAPI.Reflection;
using Humanizer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

namespace BetterAPI.OpenApi
{
    /// <summary>
    ///     Provides extended OpenAPI documentation and annotations for middleware-provided features.
    /// </summary>
    internal sealed class DocumentationOperationFilter : IOperationFilter
    {
        private readonly TypeRegistry _registry;
        private readonly IStringLocalizer<DocumentationOperationFilter> _localizer;
        private readonly IResourceDataService _service; // FIXME: this currently only refers to the first service added; it could be different!
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
            EnsureOperationHasId(operation, context, out var resourceType);

            DocumentAuthentication(operation);
            DocumentFeatures(operation, context);
            DocumentActions(operation, context, resourceType);
            DocumentResponses(operation);
            DocumentParameters(operation);
            DocumentSchemas(context);
        }
        
        private void DocumentAuthentication(OpenApiOperation operation)
        {
            operation.Responses.Add(Constants.Status401UnauthorizedString, new OpenApiResponse { Description = _localizer.GetString("Unauthorized") });
            operation.Responses.Add(Constants.Status403ForbiddenString, new OpenApiResponse { Description = _localizer.GetString("Forbidden") });
            var scheme = new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = JwtBearerDefaults.AuthenticationScheme }
            };
            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement
                {
                    [ scheme ] = new List<string>()
                }
            };
        }

        private void DocumentParameters(OpenApiOperation operation)
        {
            foreach (var parameter in operation.Parameters)
            {
                // FIXME:
                // There doesn't seem to be a way to SwaggerUI to have both a description/example, and a default value,
                // or a way to customize the Example value where it is only used as the UIs placeholder and doesn't
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

        private void DocumentActions(OpenApiOperation operation, OperationFilterContext context, Type? resourceType)
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
                if (operation.OperationId.StartsWith(Constants.Patch))
                {
                    // We want to ignore the merge patch wrapper and present it as the inner resource type
                    //
                    if (resourceType != default && context.SchemaRepository.Schemas.TryGetValue(resourceType.Name, out var schema))
                    {
                        // FIXME: Why is the XML-versioned content missing?
                        // FIXME: Why are the JSON-Patch content items missing?
                        if (!context.SchemaRepository.Schemas.TryGetValue(nameof(JsonPatch), out var patch))
                            patch = context.SchemaGenerator.GenerateSchema(typeof(JsonPatch), context.SchemaRepository);

                        var (key, value) = operation.RequestBody.Content.First();
                        value.Schema = schema;
                        
                        switch (_options.CurrentValue.ApiFormats)
                        {
                            case ApiSupportedMediaTypes.None:
                                throw new NotSupportedException(_localizer.GetString("API must support at least one content format"));
                            case ApiSupportedMediaTypes.ApplicationJson | ApiSupportedMediaTypes.ApplicationXml:
                                operation.RequestBody.Content.Add(key.Replace("json", "xml"), new OpenApiMediaType { Schema = schema });

                                operation.RequestBody.Content.Add(ApiMediaTypeNames.Application.JsonPatchJson, new OpenApiMediaType { Schema = patch});
                                operation.RequestBody.Content.Add(ApiMediaTypeNames.Application.JsonPatchXml, new OpenApiMediaType { Schema = patch });
                                break;
                            case ApiSupportedMediaTypes.ApplicationJson:
                                operation.RequestBody.Content.Add(ApiMediaTypeNames.Application.JsonPatchJson, new OpenApiMediaType { Schema = patch});
                                break;
                            case ApiSupportedMediaTypes.ApplicationXml:
                                operation.RequestBody.Content.Add(key.Replace("json", "xml"), new OpenApiMediaType { Schema = schema });
                                operation.RequestBody.Content.Add(ApiMediaTypeNames.Application.JsonPatchXml, new OpenApiMediaType { Schema = patch});
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
                else
                {
                    // '*'-based media types are confusing, so we'll remove them
                    var content = operation.RequestBody.Content.Where(x => !x.Key.Contains('*', StringComparison.OrdinalIgnoreCase)).ToList();
                    operation.RequestBody.Content.Clear();
                    foreach (var entry in content)
                    {
                        operation.RequestBody.Content.Add(entry);
                    }
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
                        // This is to disambiguate when a client (i.e. SwaggerUI) follows a redirect and would receive a misleading documented action as a result.
                        response.Description = _localizer.GetString(operation.OperationId.StartsWith(Constants.Create)
                            ? "The client followed a redirect via 303 See Other, and retrieved the equivalent, cacheable resource."
                            : "The operation succeeded, and there is additional content returned in the response.");
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
                    if (response.Description == null || response.Description == _localizer.GetString("Redirect"))
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
                    ProblemDetailsMediaType(response);

                    if (response.Description == null || response.Description == _localizer.GetString("Bad Request"))
                    {
                        response.Description = _localizer.GetString("There was an error with the request, and further problem details are available.");
                    }
                }

                if (statusCode == Constants.Status401UnauthorizedString)
                {
                    ProblemDetailsMediaType(response);

                    if (response.Description == null || response.Description == _localizer.GetString("Unauthorized"))
                    {
                        response.Description = _localizer.GetString("The resource is protected, and the provided credentials are invalid.");
                    }
                }

                if (statusCode == Constants.Status403ForbiddenString)
                {
                    ProblemDetailsMediaType(response);

                    if (response.Description == null || response.Description == _localizer.GetString("Forbidden"))
                    {
                        response.Description = _localizer.GetString("The resource is protected, and the provided credentials are not allowed to access it.");
                    }
                }

                if (statusCode == Constants.Status404NotFoundString)
                {
                    ProblemDetailsMediaType(response);

                    if (response.Description == null || response.Description == _localizer.GetString("Not Found"))
                    {
                        response.Description = _localizer.GetString("There is no resource with the specified ID on this server.");
                    }
                }

                if (statusCode == Constants.Status410GoneString)
                {
                    ProblemDetailsMediaType(response);

                    if (response.Description == null || response.Description == _localizer.GetString("Client Error"))
                    {
                        response.Description = _localizer.GetString("The resource was already deleted from the server.");
                    }
                }

                if (statusCode == Constants.Status412PreconditionFailedString)
                {
                    ProblemDetailsMediaType(response);

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
                    ProblemDetailsMediaType(response);

                    if (response.Description == null || response.Description == _localizer.GetString("Client Error"))
                    {
                        response.Description = _localizer.GetString("Requested page size was larger than the server's maximum page size.");
                    }
                }

                if (statusCode == Constants.Status500InternalServerErrorString)
                {
                    ProblemDetailsMediaType(response);

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

        private void ProblemDetailsMediaType(OpenApiResponse response)
        {
            var reference = response.Content.Values.FirstOrDefault();
            if (reference == default)
                return;

            // API versioning replaces media types with incorrect values for ProblemDetails
            response.Content.Clear();

            // See: https://tools.ietf.org/html/rfc7807#section-6.2
            if (_options.CurrentValue.ApiFormats.HasFlagFast(ApiSupportedMediaTypes.ApplicationJson))
                response.Content[ApiMediaTypeNames.Application.ProblemJson] = reference;

            if (_options.CurrentValue.ApiFormats.HasFlagFast(ApiSupportedMediaTypes.ApplicationXml))
                response.Content[ApiMediaTypeNames.Application.ProblemXml] = reference;
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

        private void EnsureOperationHasId(OpenApiOperation operation, OperationFilterContext context, out Type? resourceType)
        {
            // Assign a unique operation ID if one wasn't set by the developer
            if (string.IsNullOrWhiteSpace(operation.OperationId))
                operation.OperationId = CreateOperationId(context, out resourceType);
            else
                resourceType = default;
        }

        private string CreateOperationId(OperationFilterContext context, out Type? resourceType)
        {
            var method = context.MethodInfo;

            if (method.ReturnType == typeof(void))
            {
                resourceType = default;
                return method.Name;
            }

            resourceType = default;
            string? operationId;

            var sb = Pooling.StringBuilderPool.Get();
            try
            {
                if (method.Name.StartsWith(Constants.Get))
                    sb.Append(Constants.Get);
                else if (method.Name.StartsWith(Constants.Create))
                    sb.Append(Constants.Create);
                else if (method.Name.StartsWith(Constants.Update))
                    sb.Append(Constants.Update);
                else if (method.Name.StartsWith(Constants.Delete))
                    sb.Append(Constants.Delete);
                else if (method.Name.StartsWith(Constants.Patch))
                    sb.Append(Constants.Patch);

                var plural = false;

                foreach (var producesResponseType in context.ApiDescription.ActionDescriptor.FilterDescriptors
                    .Select(x => x.Filter)
                    .OfType<ProducesResponseTypeAttribute>())
                {
                    if (producesResponseType.Type == typeof(void))
                        continue;
                    if (producesResponseType.StatusCode <= 199 || producesResponseType.StatusCode >= 299)
                        continue;

                    resourceType = GetModelType(producesResponseType.Type, out plural);
                    break;
                }

                var parameters = method.GetParameters();

                if (resourceType == default)
                    foreach (var parameter in parameters)
                    {
                        var attributes = parameter.GetCustomAttributes(true);

                        foreach (var attribute in attributes)
                        {
                            if (!(attribute is FromBodyAttribute))
                                continue;
                            resourceType = GetModelType(parameter.ParameterType, out plural);
                            break;
                        }
                    }

                resourceType ??= GetModelType(method.ReturnType, out plural);
                sb.Append(plural ? resourceType.Name.Pluralize() : resourceType.Name);

                if (parameters.Any(x =>
                    x.Name != null && x.Name.Equals(nameof(IResource.Id), StringComparison.OrdinalIgnoreCase)))
                    sb.Append("ById");

                if (parameters.Any(x =>
                    x.Name != null && x.Name.Equals("continuationToken", StringComparison.OrdinalIgnoreCase)))
                    sb.Append("NextPage");

                operationId = sb.ToString();
            }
            finally
            {
                Pooling.StringBuilderPool.Return(sb);
            }

            _logger.LogDebug("OperationId: {OperationId}", operationId);
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
                   operation.OperationId.StartsWith(Constants.Patch, StringComparison.OrdinalIgnoreCase) ||
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

            if (definition == typeof(JsonMergePatch<>))
            {
                plural = false;
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