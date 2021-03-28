// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;

namespace BetterAPI.Reflection
{
    internal sealed class LateBoundTypeWriteAccessor : ITypeWriteAccessor
    {
        private readonly IDictionary<string, Action<object, object>> _binding;

        public LateBoundTypeWriteAccessor(AccessorMembers members)
        {
            Type = members.DeclaringType;
            _binding = LateBinding.DynamicMethodBindSet(members);
        }

        public object this[object target, string key]
        {
            set => _binding[key](target, value);
        }

        public bool TrySetValue(object? target, string key, object value)
        {
            if (target == default)
                return false;
            var bound = _binding.TryGetValue(key, out var setter);
            if (bound)
                setter?.Invoke(target, value);
            return bound;
        }

        public Type Type { get; }
    }
}