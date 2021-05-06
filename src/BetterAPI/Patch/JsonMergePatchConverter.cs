// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using BetterAPI.Reflection;

namespace BetterAPI.Patch
{
    public sealed class JsonMergePatchConverter<T> : JsonConverter<JsonMergePatch<T>> where T : class
    {
        private readonly AccessorMembers _members;

        public JsonMergePatchConverter()
        {
            _members = AccessorMembers.Create(typeof(T), AccessorMemberTypes.Properties, AccessorMemberScope.Public);
        }

        public override JsonMergePatch<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var model = new JsonMergePatch<T>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return model; // success: end of object

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException(); // fail: did not pass through previous property value

                var key = reader.GetString();
                
                if (string.IsNullOrWhiteSpace(key) || !_members.TryGetValue(key, out var member) || !member.CanRead)
                    continue;

                var value = JsonSerializer.Deserialize(ref reader, member.Type, options);
                model.TrySetPropertyValue(key, value);
            }

            // fail: passed through JsonTokenType.EndObject
            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, JsonMergePatch<T> value, JsonSerializerOptions options)
        {
            throw new NotSupportedException("If you hit this error, you're trying to write out the patch, not the model it was applied to!");
        }
    }
}