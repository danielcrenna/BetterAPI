﻿// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;

namespace BetterAPI.Reflection
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<T> StableOrder<T>(this IEnumerable<T> enumerable, Func<T, string> getName)
        {
            return enumerable.OrderBy(getName, StringComparer.Ordinal);
        }
    }
}