// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Text.Json;
using System.Text.Json.Serialization;
using BetterAPI.Reflection;

namespace BetterAPI.DeltaQueries
{
    public readonly struct DeltaAnnotated<T> : IAnnotated
    {
        public T Data { get; }

        [JsonPropertyName("@deltaLink")] public string? DeltaLink { get; }

        public DeltaAnnotated(T data, string? deltaLink)
        {
            Data = data;
            DeltaLink = deltaLink;
        }

        public void WriteInner(Utf8JsonWriter writer, IAnnotated value, JsonSerializerOptions options)
        {
            var reader = ReadAccessor.Create(typeof(T), AccessorMemberTypes.Properties, AccessorMemberScope.Public, out var members);
            JsonDeltaConverter<T>.WriteInner(members, reader, writer, (DeltaAnnotated<T>) value, options);
        }
    }
}