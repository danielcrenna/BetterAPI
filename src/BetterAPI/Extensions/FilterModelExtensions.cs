// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Linq;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace BetterAPI.Extensions
{
    internal static class FilterModelExtensions
    {
        public static void Produces(this IFilterModel model, string contentType)
        {
            if (model.Filters.Any(x => x is ProducesAttribute a && a.ContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase)))
                return;
            var produces = new ProducesAttribute(MediaTypeNames.Application.Json);
            model.Filters.Add(produces);
        }

        public static void Consumes(this IFilterModel model, string contentType)
        {
            if (model.Filters.Any(x => x is ConsumesAttribute a && a.ContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase)))
                return;
            var produces = new ConsumesAttribute(MediaTypeNames.Application.Json);
            model.Filters.Add(produces);
        }

        public static void ProducesResponseType(this IFilterModel model, int statusCode)
        {
            if (model.Filters.Any(x => x is ProducesResponseTypeAttribute a && a.Type == typeof(void) && a.StatusCode == statusCode))
                return;
            var producesResponse = new ProducesResponseTypeAttribute(typeof(void), statusCode);
            model.Filters.Add(producesResponse);
        }

        public static void ProducesResponseType<T>(this IFilterModel model, int statusCode)
        {
            if (model.Filters.Any(x => x is ProducesResponseTypeAttribute a && a.Type == typeof(T) && a.StatusCode == statusCode))
                return;
            var producesResponse = new ProducesResponseTypeAttribute(typeof(T), statusCode);
            model.Filters.Add(producesResponse);
        }

        public static void ProducesResponseType(this IFilterModel model, Type type, int statusCode)
        {
            if (model.Filters.Any(x => x is ProducesResponseTypeAttribute a && a.Type == type && a.StatusCode == statusCode))
                return;
            var producesResponse = new ProducesResponseTypeAttribute(type, statusCode);
            model.Filters.Add(producesResponse);
        }
    }
}