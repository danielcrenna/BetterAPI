// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using BetterAPI.Reflection;

namespace BetterAPI.Metrics.Internal
{
    internal static class Base36
    {
        private const int Base = 36;

        private static readonly char[] Chars =
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k',
            'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'
        };

        public static string ToBase36(this ulong input)
        {
            return Pooling.StringBuilderPool.Scoped(sb =>
            {
                while (input != 0)
                {
                    sb.Append(Chars[input % Base]);
                    input /= Base;
                }
            });
        }

        public static ulong FromBase36(this string input)
        {
            var result = 0UL;
            var pos = 0;
            for (var i = input.Length - 1; i >= 0; i--)
            {
                result += input[i] * (ulong) Math.Pow(Base, pos);
                pos++;
            }

            return result;
        }
    }
}