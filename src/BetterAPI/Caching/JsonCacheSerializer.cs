// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Primitives;

namespace BetterAPI.Caching
{
    public sealed class JsonCacheSerializer : ICacheSerializer, ICacheDeserializer
    {
        public T BufferToObject<T>(ReadOnlySpan<byte> buffer)
        {
            return JsonBufferToInstance<T>(buffer);
        }

        public void ObjectToBuffer<T>(T value, ref Span<byte> buffer, ref int startAt)
        {
            buffer.WriteString(ref startAt, new StringValues(JsonSerializer.Serialize(value)));
        }

        private static unsafe T JsonBufferToInstance<T>(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length == 0)
                return default!;

            fixed (byte* b = &bytes.GetPinnableReference())
            {
                var json = Encoding.UTF8.GetString(b, bytes.Length);

                return JsonSerializer.Deserialize<T>(json)!;
            }
        }
    }
}