// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;

namespace BetterAPI.Guidelines.Reflection
{
    internal sealed class LateBoundTypeReadAccessor : ITypeReadAccessor
    {
        private readonly IDictionary<string, Func<object, object>> _binding;

        public LateBoundTypeReadAccessor(AccessorMembers members)
        {
            Type = members.DeclaringType;
            _binding = LateBinding.DynamicMethodBindGet(members);
        }

        public object this[object target, string key] => _binding[key](target);

        public bool TryGetValue(object target, string key, out object value)
        {
            var bound = _binding.TryGetValue(key, out var getter);
            value = bound ? getter(target) : default;
            return bound;
        }

        public Type Type { get; }
    }
}