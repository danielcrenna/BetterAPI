// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using BetterAPI.Guidelines.Reflection;
using Xunit;

namespace Demo.Tests
{
    public static class Id
    {
        public static object GetId(this object model)
        {
            var accessor = ReadAccessor.Create(model);
            Assert.True(accessor.TryGetValue(model, "Id", out var id));
            return id;
        }
    }
}