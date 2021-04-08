using System;
using System.Collections.Generic;
using System.Numerics;

namespace BetterAPI.Extensions
{
    internal static class TypeExtensions
    {
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

        private static readonly HashSet<Type> RealNumberTypes = new HashSet<Type>
        {
            typeof (float),
            typeof (float?),
            typeof (double),
            typeof (double?),
            typeof (decimal),
            typeof (decimal?),
            typeof (Complex),
            typeof (Complex?)
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
    }
}
