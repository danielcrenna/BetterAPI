// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text.Json;
using BetterAPI.Shaping;
using Xunit;

namespace BetterAPI.Tests.Shaping
{
    public class SerializationTests
    {
        [Fact]
        public void Can_serialize_shaped_model()
        {
            var model = new Foo();
            var wrapper = new ShapedData<Foo>(model);

            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonShapedDataConverter<Foo>());

            var serialized = JsonSerializer.Serialize(wrapper, options);
            Assert.NotNull(serialized);
            Assert.NotEmpty(serialized);

            // Converter round-trip
            var deserialized = JsonSerializer.Deserialize<ShapedData<Foo>>(serialized, options);
            Assert.NotNull(deserialized.Data);
            Assert.Equal(model.Id, deserialized.Data.Id);
            Assert.Equal(model.Bar, deserialized.Data.Bar);
            Assert.NotNull(deserialized.Data);
        }

        public sealed class Foo
        {
            public Guid Id { get; set; }
            public string Bar { get; set; }
        }
    }
}