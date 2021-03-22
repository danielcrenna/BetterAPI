﻿using System;
using System.Reflection;

namespace BetterAPI.Guidelines.Reflection
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