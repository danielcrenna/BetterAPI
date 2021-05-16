// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using BetterAPI.Caching;
using BetterAPI.Enveloping;
using BetterAPI.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Localization;
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
        private readonly TypeRegistry _registry;
        private readonly ChangeLogBuilder _builder;
        private readonly IStringLocalizer<ApiGuidelinesConvention> _localizer;
        private readonly IOptions<ApiOptions> _options;

        public ApiGuidelinesConvention(TypeRegistry registry, ChangeLogBuilder builder, IStringLocalizer<ApiGuidelinesConvention> localizer, IOptions<ApiOptions> options)
        {
            _registry = registry;
            _builder = builder;
            _localizer = localizer;
            _options = options;
        }

        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                controller.ControllerName = controller.ControllerType.NormalizeResourceControllerName();

                if (controller.ControllerType.IsGenericType)
                {
                    if (_builder.TryGetResourceName(controller.ControllerType.GetGenericArguments()[0],
                        out var resourceName))
                    {
                        controller.ControllerName = resourceName;
                    }
                }

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
            Produces(action);

            if (action.Is(HttpMethod.Get))
            {
                if (action.ActionName.Equals(Constants.GetById, StringComparison.OrdinalIgnoreCase))
                {
                    // invalid id:
                    action.ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest);

                    // resource not found:
                    action.ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound);

                    if (_registry.TryGetValue(action.Controller.ControllerName, out var controllerType) && controllerType != default)
                    {
                        // get resource by ID with return=representation:
                        action.ProducesResponseType(controllerType, StatusCodes.Status200OK); 
                    }
                }

                if (action.ActionName.Equals(Constants.Get, StringComparison.OrdinalIgnoreCase))
                {
                    if (_registry.TryGetValue(action.Controller.ControllerName, out var controllerType) && controllerType != default)
                    {
                        // FIXME: @nextLink, @deltaLink, should be present, too
                        var collectionType = typeof(Envelope<>).MakeGenericType(controllerType);

                        // get resource collection with return=representation:
                        action.ProducesResponseType(collectionType, StatusCodes.Status200OK); 

                        // get resource collection specifying a $top operator larger than the server maximum
                        action.ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge);
                    }
                }

                if (action.ActionName.Equals(Constants.GetNextPage, StringComparison.OrdinalIgnoreCase))
                {
                    if (_registry.TryGetValue(action.Controller.ControllerName, out var controllerType) && controllerType != default)
                    {
                        // FIXME: @nextLink, @deltaLink, should be present, too
                        var collectionType = typeof(Envelope<>).MakeGenericType(controllerType);

                        // get resource collection with return=representation:
                        action.ProducesResponseType(collectionType, StatusCodes.Status200OK); 
                    }
                }

                if (!action.HasAttribute<DoNotHttpCacheAttribute>())
                    action.ProducesResponseType(StatusCodes.Status304NotModified);
            }

            if (action.Is(HttpMethod.Post))
            {
                Consumes(action);

                if (action.ActionName.Equals(Constants.Create, StringComparison.OrdinalIgnoreCase))
                {
                    // created resource with return=minimal:
                    action.ProducesResponseType(StatusCodes.Status201Created);

                    // tried to create a resource that already exists, and the supplied resource was equivalent:
                    action.ProducesResponseType<ProblemDetails>(StatusCodes.Status303SeeOther);

                    // client followed a redirect via 303 See Other, and retrieved the cacheable resource
                    action.ProducesResponseType(StatusCodes.Status200OK);

                    // invalid body
                    // tried to create a resource that already exists, and the supplied resource was not equivalent
                    action.ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest);

                    // server failed to save resource:
                    action.ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError);
                }

                if (!action.HasAttribute<DoNotHttpCacheAttribute>())
                    action.ProducesResponseType(StatusCodes.Status412PreconditionFailed);
            }

            if (action.Is(HttpMethod.Put))
            {
                Consumes(action);

                // updated resource with return=minimal:
                action.ProducesResponseType(StatusCodes.Status204NoContent);

                // successful update operation:
                action.ProducesResponseType(StatusCodes.Status200OK);
            }

            if (action.Is(HttpMethod.Patch))
            { 
                // successful patch operation:
                if (_registry.TryGetValue(action.Controller.ControllerName, out var controllerType) && controllerType != default)
                {
                    // patched resource by ID with return=representation:
                    action.ProducesResponseType(controllerType, StatusCodes.Status200OK); 
                }

                // patched resource by ID with return=minimal:
                action.ProducesResponseType(StatusCodes.Status204NoContent);

                // resource not found:
                action.ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound);

                // invalid patch operation (tried to modify a read-only field, etc.):
                action.ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest);

                switch (_options.Value.ApiFormats)
                {
                    case ApiSupportedMediaTypes.None:
                        throw new NotSupportedException(_localizer.GetString("API must support at least one content format"));
                    case ApiSupportedMediaTypes.ApplicationJson | ApiSupportedMediaTypes.ApplicationXml:
                        action.Consumes(ApiMediaTypeNames.Application.JsonMergePatch,
                            ApiMediaTypeNames.Application.XmlMergePatch,
                            ApiMediaTypeNames.Application.JsonPatchJson,
                            ApiMediaTypeNames.Application.JsonPatchXml);
                        break;
                    case ApiSupportedMediaTypes.ApplicationJson:
                        action.Consumes(ApiMediaTypeNames.Application.JsonMergePatch, ApiMediaTypeNames.Application.JsonPatchJson);
                        break;
                    case ApiSupportedMediaTypes.ApplicationXml:
                        action.Consumes(ApiMediaTypeNames.Application.XmlMergePatch, ApiMediaTypeNames.Application.JsonPatchXml);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (action.Is(HttpMethod.Delete))
            {
                if (action.ActionName.Equals(Constants.DeleteById, StringComparison.OrdinalIgnoreCase))
                {
                    // deleted resource with return=minimal:
                    action.ProducesResponseType(StatusCodes.Status204NoContent);

                    // invalid id:
                    action.ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest);

                    // resource not found:
                    action.ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound);

                    // resource already deleted:
                    action.ProducesResponseType<ProblemDetails>(StatusCodes.Status410Gone);

                    // server failed to delete resource:
                    action.ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError);

                    if (_registry.TryGetValue(action.Controller.ControllerName, out var controllerType) && controllerType != default)
                    {
                        // deleted resource by ID with return=representation:
                        action.ProducesResponseType(controllerType, StatusCodes.Status200OK); 
                    }
                }

                if (!action.HasAttribute<DoNotHttpCacheAttribute>())
                    action.ProducesResponseType(StatusCodes.Status412PreconditionFailed);
            }

            foreach (var parameter in action.Parameters)
                Apply(parameter);
        }

        public void Apply(ParameterModel parameter)
        {
            if (parameter.Action.Is(HttpMethod.Post))
            {
                foreach (var attribute in parameter.Attributes)
                {
                    if (attribute is not FromBodyAttribute || !parameter.Action.ActionName.Equals(Constants.Create, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // created resource with return=representation:
                    parameter.Action.ProducesResponseType(parameter.ParameterType, StatusCodes.Status201Created);
                    break;
                }
            }
        }

        private void Produces(IFilterModel model)
        {
            switch (_options.Value.ApiFormats)
            {
                case ApiSupportedMediaTypes.None:
                    throw new NotSupportedException(_localizer.GetString("API must support at least one content format"));
                case ApiSupportedMediaTypes.ApplicationJson | ApiSupportedMediaTypes.ApplicationXml:
                    model.Produces(ApiMediaTypeNames.Application.Json, ApiMediaTypeNames.Application.Xml);
                    break;
                case ApiSupportedMediaTypes.ApplicationJson:
                    model.Produces(ApiMediaTypeNames.Application.Json);
                    break;
                case ApiSupportedMediaTypes.ApplicationXml:
                    model.Produces(ApiMediaTypeNames.Application.Xml);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Consumes(IFilterModel model)
        {
            switch (_options.Value.ApiFormats)
            {
                case ApiSupportedMediaTypes.None:
                    throw new NotSupportedException(_localizer.GetString("API must support at least one content format"));
                case ApiSupportedMediaTypes.ApplicationJson | ApiSupportedMediaTypes.ApplicationXml:
                    model.Consumes(ApiMediaTypeNames.Application.Json, ApiMediaTypeNames.Application.Xml);
                    break;
                case ApiSupportedMediaTypes.ApplicationJson:
                    model.Consumes(ApiMediaTypeNames.Application.Json);
                    break;
                case ApiSupportedMediaTypes.ApplicationXml:
                    model.Consumes(ApiMediaTypeNames.Application.Xml);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}