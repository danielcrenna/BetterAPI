// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BetterAPI.DataProtection
{
    /// <summary>
    ///     <seealso href="https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to?pivots=dotnet-5-0#sample-factory-pattern-converter" />
    /// </summary>
    public sealed class PolicyProtectionJsonConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType)
                return false;
            return true;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var underlyingType = typeToConvert.GetGenericArguments()[0];
            var converterType = typeof(PolicyProtectionJsonConverter<>).MakeGenericType(underlyingType) ?? throw new NullReferenceException();
            JsonConverter converter = (JsonConverter) Activator.CreateInstance(converterType)! ?? throw new NullReferenceException();
            return converter;
        }
    }
}