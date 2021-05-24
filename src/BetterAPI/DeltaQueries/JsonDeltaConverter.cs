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
using BetterAPI.Shaping;

namespace BetterAPI.DeltaQueries
{
    /// <summary> Serializes a flattened object so that delta annotations can appear on any outgoing model. </summary>
    public sealed class JsonDeltaConverter<T> : JsonConverter<DeltaAnnotated<T>>
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly string DeltaLinkName;

        private readonly AccessorMembers _members;
        private readonly ITypeReadAccessor _reader;
        private readonly ITypeWriteAccessor _writer;

        static JsonDeltaConverter()
        {
            var delta = AccessorMembers.Create(typeof(DeltaAnnotated<T>), AccessorMemberTypes.Properties,
                AccessorMemberScope.Public) ?? throw new InvalidOperationException();

            if (!delta.TryGetValue(nameof(DeltaAnnotated<T>.DeltaLink), out var deltaLink))
                throw new InvalidOperationException();

            if (!deltaLink.TryGetAttribute<JsonPropertyNameAttribute>(out var attribute))
                throw new InvalidOperationException();

            DeltaLinkName = attribute.Name;
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

            T data;
            if (typeof(T).ImplementsGeneric(typeof(Many<>)))
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

            string? link = default;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new DeltaAnnotated<T>(data, link); // success: end of object

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException(); // fail: did not pass through previous property value

                var key = reader.GetString();

                if (key != null && key.Equals(DeltaLinkName, StringComparison.OrdinalIgnoreCase))
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
            if (value.Data is IShaped shaped)
            {
                shaped.WriteInner(writer, shaped, options);
                WriteAnnotation(writer, value);
            }
            else if (value.Data is IAnnotated annotated)
            {
                annotated.WriteInner(writer, annotated, options);
                WriteAnnotation(writer, value);
            }
            else
            {
                WriteInner(_members, _reader, writer, value, options);
            }
            writer.WriteEndObject();
        }

        internal static void WriteInner(AccessorMembers members, IReadAccessor reader, Utf8JsonWriter writer, DeltaAnnotated<T> value, JsonSerializerOptions options)
        {
            if (value.Data != null)
            {
                foreach (var member in members)
                {
                    if (!member.CanRead)
                        continue;

                    // key:
                    var propertyName = options.PropertyNamingPolicy?.ConvertName(member.Name) ?? member.Name;
                    writer.WritePropertyName(propertyName);

                    // value (can be null):
                    reader.TryGetValue(value.Data, member.Name, out var item);
                    JsonSerializer.Serialize(writer, item, options);
                }
            }

            WriteAnnotation(writer, value);
        }

        private static void WriteAnnotation(Utf8JsonWriter writer, DeltaAnnotated<T> value)
        {
            writer.WritePropertyName(DeltaLinkName);
            writer.WriteStringValue(value.DeltaLink);
        }
    }
}