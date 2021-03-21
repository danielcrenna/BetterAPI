// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace BetterApi.Guidelines
{
    public static class RuntimeAttributes
    {
        public static readonly AssemblyName Name;
        public static readonly AssemblyBuilder Builder;
        public static readonly ModuleBuilder Module;

        static RuntimeAttributes()
        {
            Name = new AssemblyName("__RuntimeAttributes");
            Builder = AssemblyBuilder.DefineDynamicAssembly(Name, AssemblyBuilderAccess.Run);
            Module = Builder.DefineDynamicModule(Name?.Name ?? throw new NullReferenceException());
        }

        public static TClass AddAttributeToClass<TClass, TAttribute>(params object[] args) where TClass : class
        {
            var typeBuilder = Module.DefineType($"{typeof(TClass).FullName}_Wrapper", TypeAttributes.Public, typeof(TClass));
                
            var builder = new CustomAttributeBuilder(typeof(TAttribute).GetConstructors().First(), args);
            typeBuilder.SetCustomAttribute(builder);

            var wrapperType = typeBuilder.CreateType();
            if(wrapperType == default)
                throw new NullReferenceException();

            var instance = Activator.CreateInstance(wrapperType);
            if(instance == null)
                throw new NullReferenceException();

            return instance as TClass ?? throw new NullReferenceException();
        }
    }
}