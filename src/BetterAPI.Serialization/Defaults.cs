// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using BetterAPI.Reflection;

namespace BetterAPI.Serialization
{
    internal class Defaults
    {
        public static readonly ITypeResolver TypeResolver = new ReflectionTypeResolver();
        public static readonly IValueHashProvider ValueHashProvider = new WyHashValueHashProvider();
        public static readonly IObjectSerializer ObjectSerializer = new WireSerializer();
        public static readonly IObjectDeserializer ObjectDeserializer = new WireDeserializer();
        public static readonly IStringSerializer StringSerializer = new JsonStringSerializer();
    }
}