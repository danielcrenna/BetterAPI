// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Numerics;

namespace BetterAPI
{
    public static class TypeExtensions
    {
        private static readonly HashSet<Type> RealNumberTypes = new HashSet<Type>
        {
            typeof(float),
            typeof(float?),
            typeof(double),
            typeof(double?),
            typeof(decimal),
            typeof(decimal?),
            typeof(Complex),
            typeof(Complex?)
        };

        private static readonly HashSet<Type> IntegerTypes = new HashSet<Type>
        {
            typeof(sbyte),
            typeof(sbyte?),
            typeof(byte),
            typeof(byte?),
            typeof(ushort),
            typeof(ushort?),
            typeof(short),
            typeof(short?),
            typeof(uint),
            typeof(uint?),
            typeof(int),
            typeof(int?),
            typeof(ulong),
            typeof(ulong?),
            typeof(long),
            typeof(long?),
            typeof(BigInteger),
            typeof(BigInteger?)
        };

        private static readonly HashSet<Type> BooleanTypes = new HashSet<Type>
        {
            typeof(bool),
            typeof(bool?)
        };

        public static bool IsNumeric(this Type type)
        {
            return type.IsRealNumber() || type.IsInteger();
        }

        public static bool IsInteger(this Type type)
        {
            return IntegerTypes.Contains(type);
        }

        public static bool IsTruthy(this Type type)
        {
            return BooleanTypes.Contains(type);
        }

        public static bool IsRealNumber(this Type type)
        {
            return RealNumberTypes.Contains(type);
        }

        public static bool IsAssignableFromGeneric(this Type type, Type c)
        {
            if (!type.IsGenericType)
                return false;

            var interfaceTypes = c.GetInterfaces();

            foreach (var it in interfaceTypes)
                if (it.IsGenericType && it.GetGenericTypeDefinition() == type)
                    return true;

            if (c.IsGenericType && c.GetGenericTypeDefinition() == type)
                return true;

            var baseType = c.BaseType;
            return baseType != null && IsAssignableFromGeneric(baseType, type);
        }
    }
}