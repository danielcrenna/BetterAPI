// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IO;

namespace BetterAPI.Http.Interception
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRequestInterception(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddRequestInterception(configuration.Bind);
        }

        public static IServiceCollection AddRequestInterception(this IServiceCollection services, Action<RequestInterceptionOptions>? configureAction = default)
        {
            services.Configure(configureAction);
            services.TryAddSingleton<RecyclableMemoryStreamManager>();
            return services;
        }
    }
}