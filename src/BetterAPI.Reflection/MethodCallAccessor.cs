// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Reflection;

namespace BetterAPI.Reflection
{
    public abstract class MethodCallAccessor : IMethodCallAccessor
    {
        public string MethodName { get; set; }
        public ParameterInfo[] Parameters { get; set; }

        public abstract object Call(object target, object[] args);

        public object Call(object target, IServiceProvider serviceProvider)
        {
            var args = Pooling.Arguments.Get(Parameters.Length);

            try
            {
                for (var i = 0; i < Parameters.Length; i++)
                {
                    var parameterType = Parameters[i].ParameterType;
                    var parameter = serviceProvider.GetService(parameterType);
                    args[i] = parameter;
                }

                return Call(target, args);
            }
            finally
            {
                Pooling.Arguments.Return(args);
            }
        }
    }
}