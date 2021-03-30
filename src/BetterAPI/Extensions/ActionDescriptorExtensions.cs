// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace BetterAPI.Extensions
{
    internal static class ActionDescriptorExtensions
    {
        public static bool IsCollection(this ActionDescriptor descriptor, out Type? underlyingType)
        {
            return descriptor.ReturnsEnumerableType(out underlyingType) || underlyingType == null;
        }

        public static bool IsCollectionQuery(this ActionDescriptor descriptor, out Type? underlyingType)
        {
            foreach (var http in descriptor.ActionConstraints?.OfType<HttpMethodActionConstraint>() ??
                                 Enumerable.Empty<HttpMethodActionConstraint>())
            {
                if (http.HttpMethods.Contains(HttpMethods.Get))
                    break; // is queryable

                underlyingType = default;
                return false;
            }

            return IsCollection(descriptor, out underlyingType);
        }

        public static bool ReturnsEnumerableType(this ActionDescriptor descriptor, out Type? underlyingType)
        {
            foreach (var producesResponseType in descriptor.EndpointMetadata.OfType<ProducesResponseTypeAttribute>())
            {
                if (!producesResponseType.Type.ImplementsGeneric(typeof(IEnumerable<>)))
                    continue;

                underlyingType = producesResponseType.Type.GetGenericArguments()[0];
                return true;
            }

            underlyingType = null;
            return false;
        }
    }
}