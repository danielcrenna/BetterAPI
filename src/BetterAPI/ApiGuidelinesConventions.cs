// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Linq;
using System.Net.Mime;
using BetterAPI.Caching;
using BetterAPI.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Options;

namespace BetterAPI
{
    internal sealed class ApiGuidelinesConventions : IActionModelConvention, IControllerModelConvention, IApplicationModelConvention
    {
        private readonly IOptions<ApiOptions> _options;

        public ApiGuidelinesConventions(IOptions<ApiOptions> options)
        {
            _options = options;
        }

        public void Apply(ApplicationModel application) { }

        public void Apply(ControllerModel controller) { }

        public void Apply(ActionModel action)
        {
            if (action.Is(HttpMethod.Get))
            {
                Produces(action);

                if (!action.HasAttribute<DoNotHttpCacheAttribute>())
                {
                    action.ProducesResponseType(StatusCodes.Status304NotModified);
                }
            }

            if (action.Is(HttpMethod.Post))
            {
                Produces(action);
                Consumes(action);

                if (action.ActionName.Equals("Create", StringComparison.OrdinalIgnoreCase))
                {
                    action.ProducesResponseType(StatusCodes.Status201Created);
                    action.ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest);

                    foreach (var parameter in action.Parameters)
                    {
                        foreach (var attribute in parameter.Attributes)
                        {
                            if (attribute is FromBodyAttribute)
                            {
                                action.ProducesResponseType(parameter.ParameterType, StatusCodes.Status201Created);
                            }
                        }
                    }
                }

                if (!action.HasAttribute<DoNotHttpCacheAttribute>())
                {
                    action.ProducesResponseType(StatusCodes.Status412PreconditionFailed);
                }
            }
        }

        private void Produces(IFilterModel model)
        {
            if (_options.Value.ApiFormats.HasFlagFast(ApiSupportedMediaTypes.ApplicationJson))
                model.Produces(MediaTypeNames.Application.Json);
        }

        private void Consumes(IFilterModel model)
        {
            if (_options.Value.ApiFormats.HasFlagFast(ApiSupportedMediaTypes.ApplicationJson))
                model.Consumes(MediaTypeNames.Application.Json);
        }
    }
}