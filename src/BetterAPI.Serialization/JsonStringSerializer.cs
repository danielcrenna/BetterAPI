// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace BetterAPI.Serialization
{
    public sealed class JsonStringSerializer : IStringSerializer
    {
        private static readonly JsonSerializerOptions Settings;

        static JsonStringSerializer()
        {
            Settings = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                AllowTrailingCommas = true,
                IgnoreReadOnlyProperties = false,
                ReadCommentHandling = JsonCommentHandling.Skip,
                PropertyNameCaseInsensitive = true
            };
        }

        public ReadOnlySpan<byte> ToBuffer(string text, IObjectSerializer objectSerializer, ITypeResolver typeResolver)
        {
            var hash = JsonSerializer.Deserialize<Dictionary<string, object>>(text, Settings);
            var instance = Shaping.ToAnonymousObject(hash);
            var buffer = objectSerializer.ToBuffer(instance, typeResolver);
            return buffer;
        }
    }
}