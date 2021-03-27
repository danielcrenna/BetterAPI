// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace BetterAPI.Caching
{
    internal static class CacheCalculator
    {
        private static readonly int PointerSize = IntPtr.Size;
        private static readonly int ObjectSize = PointerSize * 3;

        public static long GetMemorySize(this object obj)
        {
            long size = 0;

            Type type = obj.GetType();
            if (type.IsEnum)
            {
                size = Marshal.SizeOf(Enum.GetUnderlyingType(type));
            }
            else if (type.IsValueType)
            {
                size = Marshal.SizeOf(obj);
            }
            else if (type == typeof(string))
            {
                var str = (string) obj;
                size = str.Length * 2 + 6 + ObjectSize;
            }
            else if (type.IsArray)
            {
                var arr = (Array) obj;
                var elementType = type.GetElementType();

                if (elementType.IsEnum)
                    size += Marshal.SizeOf(Enum.GetUnderlyingType(elementType)) * arr.LongLength;
                else if (elementType.IsValueType)
                    size += Marshal.SizeOf(elementType) * arr.LongLength;
                else
                    foreach (var element in arr)
                        size += element?.GetMemorySize() + PointerSize ?? PointerSize;

                size += ObjectSize + 40;
            }
            else if (obj is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    var itemType = item.GetType();
                    size += item.GetMemorySize();
                    if (itemType.IsClass)
                        size += PointerSize;
                }

                size += ObjectSize;
            }
            else if (type.IsClass)
            {
                var properties = type.GetProperties();
                foreach (var property in properties)
                {
                    var valueObject = property.GetValue(obj);
                    size += valueObject?.GetMemorySize() ?? 0;
                    if (property.GetType().IsClass)
                        size += PointerSize;
                }

                size += ObjectSize;
            }

            return size;
        }
    }
}