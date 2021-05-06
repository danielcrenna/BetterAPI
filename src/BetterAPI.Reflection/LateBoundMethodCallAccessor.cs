// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Reflection;

namespace BetterAPI.Reflection
{
    internal sealed class LateBoundMethodCallAccessor : MethodCallAccessor
    {
        private readonly Func<object, object[], object> _binding;

        public LateBoundMethodCallAccessor(MethodInfo method)
        {
            _binding = LateBinding.DynamicMethodBindCall(method);

            MethodName = method.Name;
            Parameters = method.GetParameters();
        }

        public override object Call(object target, object[] args)
        {
            return _binding(target, args);
        }
    }
}