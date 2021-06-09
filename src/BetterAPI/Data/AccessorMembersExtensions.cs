// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Linq;
using BetterAPI.Reflection;

namespace BetterAPI.Data
{
    internal static class AccessorMembersExtensions
    {
        /// <summary>
        /// Returns only fields fit to store along with the resource (value types, and not resources or non-value collections).
        /// </summary>
        /// <param name="members"></param>
        /// <returns></returns>
        public static AccessorMember[] GetValueTypeFields(this AccessorMembers members)
        {
            return members.Where(x => !IsChildResource(x) && IsValueType(x)).ToArray();
        }

        private static bool IsValueType(AccessorMember x)
        {
            if (IsValueCollection(x))
                return true; // value collection

            return !x.Type.ImplementsGeneric(typeof(IEnumerable<>));
        }

        private static bool IsValueCollection(AccessorMember x)
        {
            return x.Type == typeof(byte[]) || x.Type == typeof(string);
        }
        
        private static bool IsChildResource(AccessorMember x)
        {
            // other resources
            return typeof(IResource).IsAssignableFrom(x.Type);
        }
    }
}