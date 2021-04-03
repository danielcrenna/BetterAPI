// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using BetterAPI.Reflection;
using Xunit;

namespace BetterAPI.Testing
{
    public static class Id
    {
        public static object GetId(this object model)
        {
            if (model is IResource resource)
                return resource.Id;

            var accessor = ReadAccessor.Create(model);
            Assert.True(accessor.TryGetValue(model, nameof(IResource.Id), out var id));
            return id;
        }

        public static object GetField(this object model, string field)
        {
            var accessor = ReadAccessor.Create(model);
            Assert.True(accessor.TryGetValue(model, field, out var id));
            return id;
        }
    }
}