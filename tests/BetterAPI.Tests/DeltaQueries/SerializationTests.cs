// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using BetterAPI.DeltaQueries;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace BetterAPI.Tests.DeltaQueries
{
    public class SerializationTests
    {
        [Fact]
        public void Can_serialize_delta_annotated_model()
        {
            var model = new Foo();
            var link = WebEncoders.Base64UrlEncode(model.Id.ToByteArray());
            var wrapper = new DeltaAnnotated<Foo>(model, link);

            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonDeltaConverter<Foo>());

            var serialized = JsonSerializer.Serialize(wrapper, options);
            Assert.NotNull(serialized);
            Assert.NotEmpty(serialized);

            // Converter round-trip
            var deserialized = JsonSerializer.Deserialize<DeltaAnnotated<Foo>>(serialized, options);
            Assert.NotNull(deserialized.Data);
            Assert.Equal(model.Id, deserialized.Data.Id);
            Assert.Equal(model.Bar, deserialized.Data.Bar);
            Assert.NotNull(deserialized.Data);
            Assert.Equal(link, deserialized.DeltaLink);
            
            // JSON projection (serializer does not need access to internal converter)
            var flattened = JsonSerializer.Deserialize<FooAnnotated>(serialized);
            Assert.NotNull(flattened);
            Assert.Equal(model.Id, flattened.Id);
            Assert.Equal(model.Bar, flattened.Bar);
            Assert.Equal(link, flattened.Link);
        }

        public sealed class Foo
        {
            public Guid Id { get; set; }
            public string Bar { get; set; }
        }

        public sealed class FooAnnotated
        {
            public Guid Id { get; set; }
            public string Bar { get; set; }

            [JsonPropertyName("@deltaLink")] public string Link { get; set; }
        }
    }
}