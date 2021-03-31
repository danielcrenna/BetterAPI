// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
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
    /// <summary>
    /// IMPORTANT: Because this isn't wrapped like the MvcOptions available in services.AddControllers(...),
    /// only the IApplicationModelConvention Apply method will fire, so we need to cascade down through the
    /// methods from top to bottom.
    /// </summary>
    internal sealed class ApiGuidelinesConvention : 
        IApplicationModelConvention,
        IControllerModelConvention,
        IActionModelConvention, 
        IParameterModelConvention
    {
        private readonly IOptions<ApiOptions> _options;

        public ApiGuidelinesConvention(IOptions<ApiOptions> options)
        {
            _options = options;
        }

        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                Apply(controller);
            }
        }
        
        public void Apply(ControllerModel controller)
        {
            foreach (var action in controller.Actions)
            {
                Apply(action);
            }
        }

        public void Apply(ActionModel action)
        {
            if (action.Is(HttpMethod.Get))
            {
                Produces(action);

                if (!action.HasAttribute<DoNotHttpCacheAttribute>())
                    action.ProducesResponseType(StatusCodes.Status304NotModified);
            }

            if (action.Is(HttpMethod.Post))
            {
                Produces(action);
                Consumes(action);

                if (action.ActionName.Equals("Create", StringComparison.OrdinalIgnoreCase))
                {
                    action.ProducesResponseType(StatusCodes.Status201Created);
                    action.ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest);
                }

                if (!action.HasAttribute<DoNotHttpCacheAttribute>())
                    action.ProducesResponseType(StatusCodes.Status412PreconditionFailed);
            }

            foreach (var parameter in action.Parameters)
                Apply(parameter);
        }

        public void Apply(ParameterModel parameter)
        {
            foreach (var attribute in parameter.Attributes)
            {
                if (attribute is not FromBodyAttribute)
                    continue;

                if (!parameter.Action.Is(HttpMethod.Post))
                    continue;

                if (!parameter.Action.ActionName.Equals("Create", StringComparison.OrdinalIgnoreCase))
                    continue;

                parameter.Action.ProducesResponseType(parameter.ParameterType, StatusCodes.Status201Created);
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