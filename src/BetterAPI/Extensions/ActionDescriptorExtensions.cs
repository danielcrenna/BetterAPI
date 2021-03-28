// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace BetterAPI.Extensions
{
    internal static class ActionDescriptorExtensions
    {
        public static bool ReturnsEnumerableType(this ActionDescriptor descriptor, out Type? type)
        {
            foreach (var producesResponseType in descriptor.EndpointMetadata.OfType<ProducesResponseTypeAttribute>())
            {
                if (!producesResponseType.Type.ImplementsGeneric(typeof(IEnumerable<>)))
                    continue;

                type = producesResponseType.Type.GetGenericArguments()[0];
                return true;
            }

            type = null;
            return false;
        }
    }
}