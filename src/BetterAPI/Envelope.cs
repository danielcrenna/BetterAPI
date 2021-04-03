// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Linq;

namespace BetterAPI
{
    public class Envelope<T>
    {
        public Envelope()
        {
            Value = Enumerable.Empty<T>().ToList();
        }

        public Envelope(IEnumerable<T> value)
        {
            Value = value?.ToList();
        }

        public List<T>? Value { get; set; }
    }
}