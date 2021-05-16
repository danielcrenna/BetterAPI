// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;

namespace BetterAPI
{
    public interface ITypeResolver
    {
        Type FindByFullName(string fullName);
        Type FindFirstByName(string name);
        Type FindFirstByMethodName(string methodName);
        IEnumerable<Type> FindByMethodName(string methodName);
        IEnumerable<Type> FindByInterface<TInterface>();
        IEnumerable<Type> FindByInterface(Type interfaceType);
        IEnumerable<Type> FindByParent<T>();
        IEnumerable<Type> FindByParent(Type parentType);
    }
}