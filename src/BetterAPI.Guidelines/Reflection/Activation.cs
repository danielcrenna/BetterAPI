﻿using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace BetterAPI.Guidelines.Reflection
{
    public static class Activation
    {
        public static CreateInstance ActivatorWeakTyped(ConstructorInfo ctor)
        {
            return args => Activator.CreateInstance(ctor.DeclaringType ?? throw new ArgumentNullException(), args);
        }

        public static CreateInstance InvokeWeakTyped(ConstructorInfo ctor)
        {
            return ctor.Invoke;
        }

        public static CreateInstance DynamicMethodWeakTyped(ConstructorInfo ctor)
        {
            var dm = new DynamicMethod($"Construct__{ctor.DeclaringType?.Assembly.GetHashCode()}_{ctor.MetadataToken}",
                ctor.DeclaringType, new[] {typeof(object[])});

            var il = dm.GetILGeneratorInternal();

            var parameters = ctor.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                il.LoadArgument(i);
                il.LoadConstant(i);
                il.Ldelem_Ref();
                il.CastOrUnboxAny(parameters[i].ParameterType);
            }

            il.Newobj(ctor);
            il.Ret();
            return (CreateInstance) dm.CreateDelegate(typeof(CreateInstance));
        }

        public static CreateInstance ExpressionWeakTyped(ConstructorInfo ctor)
        {
            var parameters = ctor.GetParameters();
            var argsParam = Expression.Parameter(typeof(object[]), "args");
            var arguments = new Expression[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
                arguments[i] = Expression.Convert(Expression.ArrayIndex(argsParam, Expression.Constant(i)),
                    parameters[i].ParameterType);
            var lambda = Expression.Lambda(typeof(CreateInstance), Expression.New(ctor, arguments), argsParam);
            var compiled = (CreateInstance) lambda.Compile();
            return compiled;
        }
    }
}