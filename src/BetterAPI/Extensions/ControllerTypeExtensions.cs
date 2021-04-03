// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace BetterAPI.Extensions
{
    internal static class ControllerTypeExtensions
    {
        public static string NormalizeResourceControllerName(this Type controllerType)
        {
            if (!controllerType.IsGenericType)
                return controllerType.NormalizeControllerName();

            return controllerType.ImplementsGeneric(typeof(ResourceController<>))
                ? controllerType.GetGenericArguments()[0].NormalizeControllerName()
                : controllerType.NormalizeControllerName();
        }

        public static string NormalizeControllerName(this Type controllerType)
        {
            return controllerType.Name.Contains('`')
                ? GetGenericControllerName(controllerType)
                : controllerType.Name.Replace(nameof(Controller), string.Empty);
        }

        public static string GetGenericControllerName(this Type controllerType)
        {
            return Pooling.StringBuilderPool.Scoped(sb =>
            {
                if (!controllerType.IsGenericType)
                {
                    sb.Append(controllerType.Name);
                    return;
                }

                var types = controllerType.GetGenericArguments();
                if (types.Length == 0)
                {
                    sb.Append(controllerType.Name);
                    return;
                }

                sb.Append(controllerType.Name.Replace($"{nameof(Controller)}`{types.Length}", string.Empty));
                foreach (var type in types)
                    sb.Append($"_{type.Name}");
            });
        }
    }
}