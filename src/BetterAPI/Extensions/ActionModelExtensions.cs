// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace BetterAPI.Extensions
{
    internal static class ActionModelExtensions
    {
        public static bool Is(this ActionModel action, HttpMethod method)
        {
            foreach (var selector in action.Selectors)
            {
                foreach (var http in selector.ActionConstraints.OfType<HttpMethodActionConstraint>())
                {
                    if (http.HttpMethods.Any(x => x.Equals(method.ToString(), StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}