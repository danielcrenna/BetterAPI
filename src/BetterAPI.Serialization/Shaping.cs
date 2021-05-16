// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using BetterAPI.Reflection;

namespace BetterAPI.Serialization
{
    public static class Shaping
    {
        public static object ToAnonymousObject(IDictionary<string, object> hash)
        {
            var anonymousType = TypeFactory.BuildAnonymousType(hash);
            var instance = Instancing.CreateInstance(anonymousType);
            var accessor = WriteAccessor.Create(anonymousType, AccessorMemberTypes.Properties,
                AccessorMemberScope.Public, out var members);

            foreach (var member in members)
            {
                if (!member.CanWrite)
                    continue; // should be accessible by design

                if (!hash.TryGetValue(member.Name, out var value))
                    continue; // should be mapped one-to-one

                accessor.TrySetValue(instance, member.Name, value);
            }

            return instance;
        }
    }
}