// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BetterAPI.DataProtection
{
    public sealed class ConfigureJsonOptions : IConfigureOptions<JsonOptions>
    {
        private readonly IAuthorizationService _authorization;
        private readonly IHttpContextAccessor _accessor;

        public ConfigureJsonOptions(IAuthorizationService authorization, IHttpContextAccessor accessor)
        {
            _authorization = authorization;
            _accessor = accessor;
        }

        public void Configure(JsonOptions options)
        {
            if(!options.JsonSerializerOptions.Converters.Any(x => x is PolicyProtectionJsonConverterFactory))
                options.JsonSerializerOptions.Converters.Add(new PolicyProtectionJsonConverterFactory(_authorization, _accessor));
        }
    }
}