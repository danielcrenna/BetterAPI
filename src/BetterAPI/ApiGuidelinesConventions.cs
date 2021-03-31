// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Linq;
using System.Net.Mime;
using BetterAPI.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace BetterAPI
{
    internal sealed class ApiGuidelinesConventions : IActionModelConvention
    {
        public void Apply(ActionModel action)
        {
            if (action.Is(HttpMethod.Get))
            {
                ProducesApplicationJson(action);
            }

            if (action.Is(HttpMethod.Post))
            {
                ProducesApplicationJson(action);
                ConsumesApplicationJson(action);
            }
        }

        private static void ProducesApplicationJson(IFilterModel model)
        {
            // [Produces(MediaTypeNames.Application.Json)]
            if (model.Filters.Any(x => x is ProducesAttribute))
                return;
            var produces = new ProducesAttribute(MediaTypeNames.Application.Json);
            model.Filters.Add(produces);
        }

        private static void ConsumesApplicationJson(IFilterModel model)
        {
            // [Consumes(MediaTypeNames.Application.Json)]
            if (model.Filters.Any(x => x is ConsumesAttribute))
                return;
            var produces = new ConsumesAttribute(MediaTypeNames.Application.Json);
            model.Filters.Add(produces);
        }
    }
}