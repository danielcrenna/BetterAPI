// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BetterAPI.Reflection
{
    public static class TypeExtensions
    {
        public static bool IsAnonymous(this Type type)
        {
            return type.Namespace == null && Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute));
        }

        public static bool HasAttribute<T>(this ICustomAttributeProvider provider, bool inherit = true)
            where T : Attribute
        {
            return provider.IsDefined(typeof(T), inherit);
        }

        public static bool TryGetAttribute<T>(this ICustomAttributeProvider provider, bool inherit, out T attribute)
            where T : Attribute
        {
            if (!provider.HasAttribute<T>())
            {
                attribute = default;
                return false;
            }

            foreach (var attr in provider.GetAttributes<T>(inherit))
            {
                attribute = attr;
                return true;
            }

            attribute = default;
            return false;
        }

        public static bool TryGetAttributes<T>(this ICustomAttributeProvider provider, bool inherit,
            out IEnumerable<T> attributes) where T : Attribute
        {
            if (!provider.HasAttribute<T>())
            {
                attributes = Enumerable.Empty<T>();
                return false;
            }

            attributes = provider.GetAttributes<T>(inherit);
            return true;
        }

        public static IEnumerable<T> GetAttributes<T>(this ICustomAttributeProvider provider, bool inherit = true)
            where T : Attribute
        {
            return provider.GetCustomAttributes(typeof(T), inherit).OfType<T>();
        }

        public static ConstructorInfo GetWidestConstructor(this Type implementationType)
        {
            return GetWidestConstructor(implementationType, out _);
        }

        public static ConstructorInfo GetWidestConstructor(this Type implementationType, out ParameterInfo[] parameters)
        {
            var allPublic = implementationType.GetConstructors();
            var constructor = allPublic.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
            if (constructor == null)
            {
                parameters = new ParameterInfo[0];
                return implementationType.GetConstructor(Type.EmptyTypes);
            }

            parameters = constructor.GetParameters();
            return constructor;
        }

        internal static bool NeedsLateBoundAccessor(this Type type, AccessorMembers members)
        {
            if (type.IsNotPublic)
                return true;

            if (type.IsNestedPublic && type.DeclaringType != null && type.DeclaringType.IsNotPublic)
                return true;

            foreach (var member in members)
                switch (member.MemberInfo)
                {
                    case FieldInfo field when !field.IsPublic:
                    case PropertyInfo property when !property.GetGetMethod(true).IsPublic:
                        return true;
                }

            return false;
        }

        public static IEnumerable<Type> GetImplementationsOfOpenGeneric(this Type openGenericType)
        {
            return GetImplementationsOfOpenGeneric(openGenericType, AppDomain.CurrentDomain.GetAssemblies());
        }

        public static IEnumerable<Type> GetImplementationsOfOpenGeneric(this Type openGenericType,
            IEnumerable<Assembly> assemblies)
        {
            if (!openGenericType.IsGenericType)
                throw new ArgumentException("The provided type is not an open generic type", nameof(openGenericType));

            foreach (var assembly in assemblies)
            foreach (var at in assembly.GetTypes())
            foreach (var t in GetImplementationsOfOpenGeneric(openGenericType, at))
                yield return t;
        }

        public static bool ImplementsGeneric(this Type type, Type generic)
        {
            while (true)
            {
                if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == generic)
                    return true;

                var interfaces = type.GetTypeInfo().ImplementedInterfaces;

                foreach (var @interface in interfaces)
                    if (@interface.IsConstructedGenericType && @interface.GetGenericTypeDefinition() == generic)
                        return true;

                if (type.BaseType == null)
                    return false;

                type = type.BaseType;
            }
        }

        public static IEnumerable<Type> GetImplementationsOfOpenGeneric(this Type openGenericType, Type type)
        {
            if (type.ImplementsGeneric(openGenericType))
                yield return type;

            foreach (var member in type.GetMembers())
            {
                if (!(member is MethodBase ctorOrMethod))
                    continue;

                foreach (var parameter in ctorOrMethod.GetParameters())
                    if (parameter.ParameterType.ImplementsGeneric(openGenericType))
                        yield return parameter.ParameterType;
            }
        }
    }
}