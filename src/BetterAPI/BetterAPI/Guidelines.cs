// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
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

            private static bool IsMutation(OpenApiOperation operation)
            {
                return operation.OperationId.StartsWith("Create", StringComparison.OrdinalIgnoreCase) ||
                       operation.OperationId.StartsWith("Update", StringComparison.OrdinalIgnoreCase);
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
            public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                if (context.HttpContext.Request.Headers.TryGetValue(Headers.Prefer, out var prefer))
                {
                    foreach (var _ in prefer.Where(token => token.Equals(Prefer.ReturnMinimal, StringComparison.OrdinalIgnoreCase)))
                    {
                        context.HttpContext.Response.Headers.Add(Headers.PreferenceApplied, Prefer.ReturnMinimal);
                        if (context.Result is ObjectResult result)
                            result.Value = default;
                    }
                }

                return Task.CompletedTask;
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
    }
}