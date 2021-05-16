// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace BetterAPI.DataProtection
{
    /// <summary>
    ///     <seealso href="https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to?pivots=dotnet-5-0#sample-factory-pattern-converter" />
    /// </summary>
    public sealed class PolicyProtectionJsonConverterFactory : JsonConverterFactory
    {
        private readonly IAuthorizationService _authorization;
        private readonly IHttpContextAccessor _accessor;

        public PolicyProtectionJsonConverterFactory(IAuthorizationService authorization, IHttpContextAccessor accessor)
        {
            _authorization = authorization;
            _accessor = accessor;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(IResource).IsAssignableFrom(typeToConvert);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            // var underlyingType = typeToConvert.GetGenericArguments()[0];
            var converterType = typeof(PolicyProtectionJsonConverter<>).MakeGenericType(typeToConvert) ?? throw new NullReferenceException();
            JsonConverter converter = (JsonConverter) Activator.CreateInstance(converterType, _authorization, _accessor)! ?? throw new NullReferenceException();
            return converter;
        }
    }
}