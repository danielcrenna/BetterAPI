// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using BetterAPI.Reflection;

namespace BetterAPI.Shaping
{
    public readonly struct ShapedData<T> : IShaped
    {
        public T Data { get; }
        public IList<string> Fields { get; }

        public ShapedData(T data, params string[] fields)
        {
            Data = data;
            Fields = fields;
        }

        public ShapedData(T data, IList<string> fields)
        {
            Data = data;
            Fields = fields;
        }

        public object? Body => Data;

        public void WriteInner(Utf8JsonWriter writer, IShaped value, JsonSerializerOptions options)
        {
            var reader = ReadAccessor.Create(typeof(T), AccessorMemberTypes.Properties, AccessorMemberScope.Public, out var members);
            JsonShapedDataConverter<T>.WriteInner(members, reader, writer, value, options);
        }
    }
}