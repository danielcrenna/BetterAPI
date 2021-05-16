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
        public static AccessorMember[] GetDiscreteFields(this AccessorMembers members)
        {
            // remove resource members, as these have their own tables
            return members.Where(x => 
                    !typeof(IResource).IsAssignableFrom(x.Type) && // other resources
                    (x.Type == typeof(string) || !x.Type.ImplementsGeneric(typeof(IEnumerable<>))) // collections
            ).ToArray();
        }
    }
}