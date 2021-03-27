// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;

namespace BetterAPI
{
    public static class ApiGuidelines
    {
        // FIXME: Use ImplementsOpenGeneric<IEnumerable<>>
        public static Type GetModelType(this Type type, out bool plural)
        {
            if (!type.IsGenericType)
            {
                plural = false;
                return type;
            }

            var definition = type.GetGenericTypeDefinition();

            if (definition == typeof(IEnumerable<>))
            {
                plural = true;
                return type.GetGenericArguments()[0];
            }

            foreach (var @interface in definition.GetInterfaces())
                if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    plural = true;
                    return type.GetGenericArguments()[0];
                }

            plural = false;
            return type;
        }
    }
}