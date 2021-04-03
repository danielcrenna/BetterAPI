// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;

namespace BetterAPI.Extensions
{
    internal static class EnumerableExtensions
    {
        public static bool Contains(this IEnumerable<string> source, IEnumerable<string> target, StringComparison comparer)
        {
            foreach (var item in source)
            {
                if (item == null)
                    continue;

                foreach (var candidate in target)
                {
                    if (candidate.Contains(item, comparer))
                        return true;
                }
            }

            return false;
        }
    }
}