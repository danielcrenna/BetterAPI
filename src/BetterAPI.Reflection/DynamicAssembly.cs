// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Reflection;
using System.Reflection.Emit;

namespace BetterAPI.Reflection
{
    internal static class DynamicAssembly
    {
        public static readonly AssemblyName Name;
        public static readonly AssemblyBuilder Builder;
        public static readonly ModuleBuilder Module;

        static DynamicAssembly()
        {
            Name = new AssemblyName("__BetterAPI");
            Builder = AssemblyBuilder.DefineDynamicAssembly(Name, AssemblyBuilderAccess.Run);
            Module = Builder.DefineDynamicModule(Name.Name);
        }
    }
}