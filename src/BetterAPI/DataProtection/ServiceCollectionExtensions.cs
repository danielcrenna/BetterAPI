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
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPolicyProtection(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddSingleton<IConfigureOptions<JsonOptions>, ConfigureJsonOptions>();
            services.AddAuthorization();
            return services;
        }
    }
}