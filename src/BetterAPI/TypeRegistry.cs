// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BetterAPI.Reflection;

namespace BetterAPI
{
    internal sealed class ResourceTypeRegistry
    {
        private readonly Type[] _loadedTypes;

        private readonly IDictionary<string, Type> _types;

        public ResourceTypeRegistry(params Assembly[] assemblies)
        {
            _types = new Dictionary<string, Type>();

            // reverse order so user types are first
            var sources = AppDomain.CurrentDomain.GetAssemblies();
            _loadedTypes = sources.Concat(assemblies).Where(a => !a.IsDynamic)
                .SelectMany(x => x.GetTypes()).Reverse().ToArray();
        }

        public bool GetOrRegisterByName(string typeName, out Type? type)
        {
            if (_types.TryGetValue(typeName, out type))
                return true;

            type = FindTypeByName(typeName);
            if (type == default)
                return false;

            _types.Add(typeName, type);
            return true;
        }

        private Type? FindTypeByName(string name)
        {
            foreach (var type in _loadedTypes)
            {
                if (type.TryGetAttribute(true, out ResourceNameAttribute resourceName) && name.Equals(resourceName.Name, StringComparison.OrdinalIgnoreCase))
                    return type;

                if (type.Name == name)
                    return type;
            }
            return default;
        }
    }
}