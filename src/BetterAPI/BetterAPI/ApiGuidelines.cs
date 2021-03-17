// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Expressions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BetterAPI
{
    public static class ApiGuidelines
    {
        private static readonly string CreatedStatus = StatusCodes.Status201Created.ToString();

        public static class Prefer
        {
            public const string ReturnMinimal = "return=minimal";
            public const string ReturnRepresentation = "return=representation";
        }

        public static class Headers
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public const string Prefer = "Prefer";
            public const string PreferenceApplied = "Preference-Applied";
        }

        internal sealed class ApiGuidelinesOperationFilter : IOperationFilter
        {
            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                // Assign a unique operation ID if one wasn't set by the developer
                if(string.IsNullOrWhiteSpace(operation.OperationId))
                    operation.OperationId = CreateOperationId(context);

                if (IsMutation(operation))
                {
                    operation.Parameters.Add(new OpenApiParameter
                    {
                        Name = "Prefer",
                        In = ParameterLocation.Header,
                        Description = "Apply a preference for the response",
                        Example = new OpenApiString("return=minimal")
                    });
                }

                foreach (var response in operation.Responses)
                    if (response.Key == CreatedStatus)
                        AddGetByIdLink(operation, response);
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

                foreach (var attribute in context.MethodInfo.GetCustomAttributes(true))
                {
                    if (!(attribute is ProducesResponseTypeAttribute producesResponseType))
                        continue;
                    if (producesResponseType.Type == typeof(void))
                        continue;
                    if (producesResponseType.StatusCode <= 199 || producesResponseType.StatusCode >= 299)
                        continue;

                    type = producesResponseType.Type.GetModelType(out plural);
                    break;
                }

                var parameters = method.GetParameters();

                if (type == default)
                {
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
                }
                
                type ??= method.ReturnType.GetModelType(out plural);
                sb.Append(plural ? type.Name.Pluralize() : type.Name);
                
                if (parameters.Any(x => x.Name != null && x.Name.Equals("id", StringComparison.OrdinalIgnoreCase)))
                    sb.Append("ById");

                return sb.ToString();
            }

            private static bool IsMutation(OpenApiOperation operation)
            {
                return operation.OperationId.StartsWith("Create", StringComparison.OrdinalIgnoreCase) ||
                       operation.OperationId.StartsWith("Update", StringComparison.OrdinalIgnoreCase) ||
                       operation.OperationId.StartsWith("Delete", StringComparison.OrdinalIgnoreCase);
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
                    Description = $"The `id` value returned in the response can be used as the `id` parameter in `GET /{modelName}/{{id}}`",
                };

                // parameters:
                //    id: '$response.body#/id'
                getById.Parameters.Add("id",
                    new RuntimeExpressionAnyWrapper
                        {Expression = new ResponseExpression(new BodyExpression(new JsonPointer("/id")))});

                response.Value.Links.Add(operationId, getById);
            }
        }

        internal sealed class ApiGuidelinesActionFilter : IAsyncActionFilter
        {
            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                var minimal = false;

                if (context.HttpContext.Request.Headers.TryGetValue(Headers.Prefer, out var prefer))
                {
                    foreach (var _ in prefer.Where(token => token.Equals(Prefer.ReturnMinimal, StringComparison.OrdinalIgnoreCase)))
                    {
                        context.HttpContext.Response.Headers.Add(Headers.PreferenceApplied, Prefer.ReturnMinimal);
                        minimal = true;
                    }
                }

                var executed = await next.Invoke();

                if (executed.Result is ObjectResult result)
                {
                    if (result.Value != default)
                    {
                        var json = JsonSerializer.Serialize(result.Value);
                        var buffer = Encoding.UTF8.GetBytes(json);
                        var etag = ETagGenerator.Generate(buffer);
                        context.HttpContext.Response.Headers.TryAdd(HeaderNames.ETag, etag.Value);
                    }

                    if (minimal)
                    {
                        result.Value = default;
                    }
                }
            }
        }

        public static IServiceCollection AddApiGuidelines(this IServiceCollection services)
        {
            services.AddControllers()
                // See: https://docs.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-5.0#use-apibehavioroptionsclienterrormapping
                .ConfigureApiBehaviorOptions(o => { });

            services.AddSingleton<ApiGuidelinesActionFilter>();

            services.AddMvc(o =>
            {
                o.Filters.AddService<ApiGuidelinesActionFilter>();
            });

            return services;
        }

        public enum ETagType
        {
            Weak,
            Strong
        }

        public readonly struct ETag
        {
            public readonly ETagType Type;
            public readonly string Value;

            public ETag(ETagType type, string value)
            {
                Type = type;
                Value = value;
            }
        }

        internal static class ETagGenerator
        {
            public static ETag Generate(ReadOnlySpan<byte> buffer)
            {
                using var md5 = MD5.Create();
                var hash = new byte[md5.HashSize/ 8];
                var hashed = md5.TryComputeHash(buffer, hash, out _);
                Debug.Assert(hashed);
                var hex = BitConverter.ToString(hash);
                return new ETag(ETagType.Weak, $"W/\"{hex.Replace("-", "")}\"");
            }
        }

        public static Type GetModelType(this Type type, out bool plural)
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
            {
                if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    plural = true;
                    return type.GetGenericArguments()[0];
                }
            }

            plural = false;
            return type;
        }
    }
}