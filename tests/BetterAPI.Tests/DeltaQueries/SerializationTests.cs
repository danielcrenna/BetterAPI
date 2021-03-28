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
            
            var flattened = JsonSerializer.Deserialize<FooAnnotated>(serialized, options);

            Assert.NotNull(flattened);
            Assert.Equal(model.Id, flattened.Id);
            Assert.Equal(model.Bar, flattened.Bar);
            Assert.Equal(link, flattened.Link);
        }

        public sealed class Foo
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public string Bar { get; set; } = "Baz";
        }

        public sealed class FooAnnotated
        {
            public Guid Id { get; set; }
            public string Bar { get; set; }

            [JsonPropertyName("@deltaLink")]
            public string Link { get; set; }
        }
    }
}
