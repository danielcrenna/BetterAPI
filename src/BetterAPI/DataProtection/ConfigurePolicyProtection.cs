// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BetterAPI.DataProtection
{
    public class ConfigurePolicyProtection : IConfigureOptions<JsonOptions>
    {
        private readonly IAuthorizationService _authorization;
        private readonly IHttpContextAccessor _http;

        public ConfigurePolicyProtection(IAuthorizationService authorization, IHttpContextAccessor http)
        {
            _authorization = authorization;
            _http = http;
        }

        public void Configure(JsonOptions options)
        {
            options.JsonSerializerOptions.Converters.Add(new PolicyProtectionJsonConverter(_authorization, _http));
        }
    }
}