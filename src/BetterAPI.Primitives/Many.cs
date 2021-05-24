// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BetterAPI
{
    public class Many<T> : IEnveloped, IEnumerable<T>
    {
        private static readonly IEnumerable<T> EmptyEnumerable = Enumerable.Empty<T>();
        private static readonly IEnumerator<T> EmptyEnumerator = EmptyEnumerable.GetEnumerator();
        
        public Many()
        {
            Value = Enumerable.Empty<T>().ToList();
        }

        public Many(IEnumerable<T> value)
        {
            Value = value?.ToList();
        }

        public List<T>? Value { get; set; }

        public int Items => Value?.Count ?? 0;
        
        public IEnumerator<T> GetEnumerator()
        {
            return Value?.GetEnumerator() ?? EmptyEnumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Value?.GetEnumerator() ?? EmptyEnumerator;
        }
    }
}