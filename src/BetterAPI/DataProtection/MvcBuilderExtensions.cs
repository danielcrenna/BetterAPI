// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BetterAPI.DataProtection
{
    public static class MvcBuilderExtensions
    {
        public static IMvcBuilder AddPolicyProtection(this IMvcBuilder builder)
        {
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<IConfigureOptions<JsonOptions>, ConfigurePolicyProtection>();
            return builder;
        }
    }
}