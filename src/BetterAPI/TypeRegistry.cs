// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;

namespace BetterAPI
{
    internal sealed class TypeRegistry
    {
        private readonly Type[] _loadedTypes;

        private readonly IDictionary<string, Type> _types;

        public TypeRegistry()
        {
            _types = new Dictionary<string, Type>();
            _loadedTypes = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic)
                .SelectMany(x => x.GetTypes()).ToArray();
        }

        public bool TryGetValue(string typeName, out Type? type)
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
                if (type.Name == name)
                    return type;
            }
            return default;
        }
    }
}