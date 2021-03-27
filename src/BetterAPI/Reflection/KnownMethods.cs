// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Reflection;

namespace BetterAPI.Reflection
{
    internal static class KnownMethods
    {
        public static readonly MethodInfo StringEquals =
            typeof(string).GetMethod("op_Equality", new[] {typeof(string), typeof(string)});

        public static readonly MethodInfo GetTypeFromHandle =
            typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), BindingFlags.Static | BindingFlags.Public);

        public static MethodInfo GetMethodFromHandle =
            typeof(MethodBase).GetMethod(nameof(MethodBase.GetMethodFromHandle), new[] {typeof(RuntimeMethodHandle)});

        public static MethodInfo GetFieldFromHandle =
            typeof(FieldInfo).GetMethod(nameof(FieldInfo.GetFieldFromHandle), new[] {typeof(RuntimeFieldHandle)});

        public static MethodInfo CallWithArgs = typeof(MethodCallAccessor).GetMethod(nameof(MethodCallAccessor.Call),
            BindingFlags.Public | BindingFlags.Instance,
            null, CallingConventions.HasThis,
            new[] {typeof(object), typeof(object[])},
            null);
    }
}