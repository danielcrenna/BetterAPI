// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BetterAPI.Filtering
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCollectionFiltering(this IServiceCollection services,
            IConfiguration configuration)
        {
            return services.AddCollectionFiltering(configuration.Bind);
        }

        public static IServiceCollection AddCollectionFiltering(this IServiceCollection services,
            Action<FilterOptions>? configureAction = default)
        {
            if (configureAction != default)
            {
                services.AddOptions();
                services.Configure(configureAction);
            }

            services.AddScoped<FilterQueryActionFilter>();
            services.AddMvc(o => { o.Filters.AddService<FilterQueryActionFilter>(int.MinValue); });
            return services;
        }
    }
}