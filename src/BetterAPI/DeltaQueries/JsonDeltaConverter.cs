// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using BetterAPI.Reflection;

namespace BetterAPI.DeltaQueries
{
    // FIXME: It might be faster to implement this as a converter factory rather than use reflection emit to create a generic wrapper.

    /// <summary>
    ///     <seealso
    ///         href="https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to?pivots=dotnet-5-0#sample-factory-pattern-converter" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JsonDeltaConverter<T> : JsonConverter<DeltaAnnotated<T>>
    {
        private static readonly string _deltaLinkName;
        private readonly AccessorMembers _members;
        private readonly ITypeReadAccessor _reader;

        private readonly ITypeWriteAccessor _writer;

        static JsonDeltaConverter()
        {
            var delta = AccessorMembers.Create(typeof(DeltaAnnotated<T>)) ?? throw new InvalidOperationException();

            if (!delta.TryGetValue(nameof(DeltaAnnotated<T>.DeltaLink), out var deltaLink))
                throw new InvalidOperationException();

            if (!deltaLink.TryGetAttribute<JsonPropertyNameAttribute>(out var attribute))
                throw new InvalidOperationException();

            _deltaLinkName = attribute.Name;
        }

        public JsonDeltaConverter()
        {
            _reader = ReadAccessor.Create(typeof(T), AccessorMemberTypes.Properties, AccessorMemberScope.Public, out _members);
            _writer = WriteAccessor.Create(typeof(T), AccessorMemberTypes.Properties, AccessorMemberScope.Public);
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.ImplementsGeneric(typeof(DeltaAnnotated<>));
        }

        public override DeltaAnnotated<T> Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

            var data = Instancing.CreateInstance<T>();
            string? link = default;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new DeltaAnnotated<T>(data, link); // success: end of object

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException(); // fail: did not pass through previous property value

                var key = reader.GetString();

                if (key != null && key.Equals(_deltaLinkName, StringComparison.OrdinalIgnoreCase))
                {
                    if(!reader.Read() || reader.TokenType != JsonTokenType.String)
                        throw new JsonException();
                        
                    link = reader.GetString();
                    continue;
                }

                if (string.IsNullOrWhiteSpace(key) || !_members.TryGetValue(key, out var member) || !member.CanWrite)
                    continue;

                var value = JsonSerializer.Deserialize(ref reader, member.Type, options);

                if (!_writer.TrySetValue(data, key, value!))
                    throw new JsonException();
            }

            // fail: passed through JsonTokenType.EndObject
            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, DeltaAnnotated<T> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            if (value.Data != null)
                foreach (var member in _members)
                {
                    if (!member.CanRead)
                        continue;

                    // key:
                    var propertyName = options.PropertyNamingPolicy?.ConvertName(member.Name) ?? member.Name;
                    writer.WritePropertyName(propertyName);

                    // value (can be null):
                    _reader.TryGetValue(value.Data, member.Name, out var item);
                    JsonSerializer.Serialize(writer, item, options);
                }

            writer.WritePropertyName(_deltaLinkName);
            writer.WriteStringValue(value.DeltaLink);
            writer.WriteEndObject();
        }
    }
}