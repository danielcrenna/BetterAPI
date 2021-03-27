// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using BetterAPI;
using Xunit;

namespace Demo.Tests
{
    public class RuntimeAttributeTests
    {
        [Fact]
        public void Can_add_attribute_to_class_with_default_constructor()
        {
            var instance = RuntimeAttributes.AddAttributeToClass<FooClass, FooAttribute>("Foo");
            Assert.NotNull(instance);
            Assert.Contains(instance.GetType().GetCustomAttributes(true), x => x is FooAttribute foo && foo.Foo == "Foo");
        }

        public class FooClass { }

        [AttributeUsage(AttributeTargets.Class)]
        public class FooAttribute : Attribute
        {
            public string Foo { get; }

            public FooAttribute(string foo)
            {
                Foo = foo;
            }
        }
    }
}