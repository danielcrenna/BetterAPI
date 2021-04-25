// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using BetterAPI.Enveloping;
using BetterAPI.Reflection;

namespace BetterAPI.Shaping
{
    /// <summary> Serializes a flattened object so that models are shaped by specific set of included fields. </summary>
    public sealed class JsonShapedDataConverter<T> : JsonConverter<ShapedData<T>>
    {
        private readonly AccessorMembers _members;
        private readonly ITypeReadAccessor _reader;
        private readonly ITypeWriteAccessor _writer;
        
        public JsonShapedDataConverter()
        {
            _reader = ReadAccessor.Create(typeof(T), AccessorMemberTypes.Properties, AccessorMemberScope.Public, out _members);
            _writer = WriteAccessor.Create(typeof(T), AccessorMemberTypes.Properties, AccessorMemberScope.Public);
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(IShapedData).IsAssignableFrom(typeToConvert) ||
                   typeToConvert.ImplementsGeneric(typeof(ShapedData<>));
        }

        public override ShapedData<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

            T data;
            if (typeof(T).ImplementsGeneric(typeof(Envelope<>)))
            {
                // FIXME: Instancing can't handle nested generic types, so we have to use manual reflection here
                //        until it is resolved.
                var underlyingType = typeof(T).GetGenericArguments()[0];
                var enumerableType = typeof(List<>).MakeGenericType(underlyingType);
                var enumerable = Activator.CreateInstance(enumerableType);
                data = (T) (Activator.CreateInstance(typeof(T), enumerable) ?? throw new NullReferenceException());
            }
            else
            { 
                data = Instancing.CreateInstance<T>();
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new ShapedData<T>(data); // success: end of object

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException(); // fail: did not pass through previous property value

                var key = reader.GetString();

                if (string.IsNullOrWhiteSpace(key) || !_members.TryGetValue(key, out var member) || !member.CanWrite)
                    continue;

                var value = JsonSerializer.Deserialize(ref reader, member.Type, options);

                if (!_writer.TrySetValue(data, key, value!))
                    throw new JsonException();
            }

            // fail: passed through JsonTokenType.EndObject
            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, ShapedData<T> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            if (value.Data != null)
                foreach (var field in value.Fields)
                {
                    if (!_members.TryGetValue(field, out var member))
                        continue;

                    if (!member.CanRead)
                        continue;

                    // key:
                    var propertyName = options.PropertyNamingPolicy?.ConvertName(member.Name) ?? member.Name;
                    writer.WritePropertyName(propertyName);

                    // value (can be null):
                    _reader.TryGetValue(value.Data, member.Name, out var item);
                    JsonSerializer.Serialize(writer, item, options);
                }
            writer.WriteEndObject();
        }
    }
}