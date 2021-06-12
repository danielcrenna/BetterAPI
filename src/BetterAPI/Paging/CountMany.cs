// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Linq;

namespace BetterAPI.Paging
{
    public sealed class CountMany<T> : Many<T>
    {
        public CountMany()
        {
            Value = Enumerable.Empty<T>().ToList();
            MaxItems = 0;
        }

        public CountMany(IEnumerable<T> value, int maxItems)
        {
            Value = value?.ToList();
            MaxItems = maxItems;
        }

        public int? MaxItems { get; set; }
    }
}