// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Expressions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BetterAPI
{
    public static class Guidelines
    {
        private static readonly string Created = StatusCodes.Status201Created.ToString();

        public static class Prefer
        {
            public const string ReturnMinimal = "return=minimal";
            public const string ReturnRepresentation = "return=representation";
        }

        public static class Headers
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public const string Prefer = nameof(Prefer);
            public const string PreferenceApplied = nameof(PreferenceApplied);
        }

        public class OperationFilter : IOperationFilter
        {
            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                foreach (var response in operation.Responses)
                    if (response.Key == Created)
                        AddGetByIdLink(operation, response);
            }

            private static void AddGetByIdLink(OpenApiOperation operation,
                KeyValuePair<string, OpenApiResponse> response)
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
        }
    }
}