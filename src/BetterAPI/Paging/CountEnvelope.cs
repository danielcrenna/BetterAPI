﻿// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Linq;
using BetterAPI.Enveloping;

namespace BetterAPI.Paging
{
    public sealed class CountEnvelope<T> : Envelope<T>
    {
        public CountEnvelope()
        {
            Value = Enumerable.Empty<T>().ToList();
        }

        public CountEnvelope(IEnumerable<T> value, int maxItems)
        {
            Value = value?.ToList();
            MaxItems = maxItems;
        }

        public int? MaxItems { get; set; }
    }
}