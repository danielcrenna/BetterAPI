// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace BetterAPI.DataProtection
{
    /// <summary>
    /// See: https://docs.microsoft.com/en-us/aspnet/core/migration/claimsprincipal-current?view=aspnetcore-5.0
    /// See: https://docs.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-5.0
    /// See: https://docs.microsoft.com/en-us/aspnet/core/security/authorization/iauthorizationpolicyprovider?view=aspnetcore-5.0
    /// </summary>
    public sealed class PolicyProtectionJsonConverter<T> : JsonConverter<T>
    {
        private readonly IAuthorizationService _authorization;
        private readonly IHttpContextAccessor? _http;

        public PolicyProtectionJsonConverter(IAuthorizationService authorization, IHttpContextAccessor? http = default)
        {
            _authorization = authorization;
            _http = http;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(IResource).IsAssignableFrom(typeToConvert);
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var writer = WriteAccessor.Create(typeToConvert, AccessorMemberTypes.Properties, AccessorMemberScope.Public, out var members);

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            var data = Activator.CreateInstance<T>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return data; // success: end of object

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException(); // fail: did not pass through previous property value

                var key = reader.GetString();
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                var propertyName =  options.PropertyNamingPolicy?.ConvertName(key);
                if (string.IsNullOrWhiteSpace(propertyName) || !members.TryGetValue(propertyName, out var member) ||
                    !member.CanWrite)
                {
                    reader.Skip();
                    continue;
                }

                if (member.TryGetAttribute<ProtectedByPolicyAttribute>(out var attribute))
                {
                    var user =  _http.ResolveCurrentPrincipal();
                    if (!user.Claims.Any())
                    {
                        reader.Skip();
                        continue;
                    }
                                        
                    // resource is set to null here, because we don't want to deserialize the protected property unless we need to
                    var result = _authorization.AuthorizeAsync(user, null, attribute.PolicyName).ConfigureAwait(false).GetAwaiter().GetResult();
                    if (!result.Succeeded)
                    {
                        reader.Skip();
                        continue;
                    }
                }

                var value = JsonSerializer.Deserialize(ref reader, member.Type, options);
                if (!writer.TrySetValue(data, member.Name, value!))
                    throw new JsonException();
            }

            // fail: passed through JsonTokenType.EndObject
            throw new JsonException();
        }

        public override async void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            var reader = ReadAccessor.Create(value, AccessorMemberTypes.Properties, AccessorMemberScope.Public, out var members);

            writer.WriteStartObject();
            foreach (var member in members)
            {
                if (!member.CanRead)
                    continue;

                if (member.TryGetAttribute<ProtectedByPolicyAttribute>(out var attribute))
                {
                    var user = _http.ResolveCurrentPrincipal();
                    if (!user.Claims.Any())
                        continue;

                    var result = await _authorization.AuthorizeAsync(user, value, attribute.PolicyName);
                    if (result.Succeeded)
                        WriteNameAndValue(member);
                }
                else
                {
                    WriteNameAndValue(member);
                }
            }
            writer.WriteEndObject();

            void WriteNameAndValue(AccessorMember member)
            {
                // key:
                var propertyName = options.PropertyNamingPolicy?.ConvertName(member.Name) ?? member.Name;
                writer.WritePropertyName(propertyName);

                // value (can be null):
                reader.TryGetValue(value, member.Name, out var item);
                JsonSerializer.Serialize(writer, item, options);
            }
        }
    }
}