// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace BetterAPI.Dashboard
{
    public class ReflectionTypeResolver : ITypeResolver
    {
        private readonly Lazy<IEnumerable<Type>> _loadedTypes;
        private readonly string[] _skipRuntimeAssemblies;

        public ReflectionTypeResolver()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            _loadedTypes = new Lazy<IEnumerable<Type>>(() => LoadTypes(assemblies, typeof(object).GetTypeInfo().Assembly));
            _skipRuntimeAssemblies = new[]
            {
                "Microsoft.VisualStudio.ArchitectureTools.PEReader",
                "Microsoft.IntelliTrace.Core, Version=16.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
            };
        }

        public Type? FindByFullName(string typeName)
        {
            foreach (var type in _loadedTypes.Value)
                if (type.FullName != null && type.FullName.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                    return type;

            return null;
        }
        
        private IEnumerable<Type> LoadTypes(IEnumerable<Assembly> assemblies, params Assembly[] skipAssemblies)
        {
            var types = new HashSet<Type>();

            foreach (var assembly in assemblies)
            {
                if (assembly.IsDynamic || ((IList) skipAssemblies).Contains(assembly) ||
                    ((IList) _skipRuntimeAssemblies).Contains(assembly.FullName))
                    continue;

                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        // Console.WriteLine(type.FullName);
                        types.Add(type);
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine($"Failed to load types in assembly {assembly.GetName().Name}");
                }
            }

            return types;
        }
    }
}