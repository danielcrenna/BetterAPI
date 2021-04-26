// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BetterAPI.Prefer
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///     Adds support for the `Prefer` header, with options for `return=minimal` and `return=representation`.
        ///     By default, the full representation is returned.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddPrefer(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddPrefer(configuration.Bind);
        }

        /// <summary>
        ///     Adds support for the `Prefer` header, with options for `return=minimal` and `return=representation`.
        ///     By default, the full representation is returned.
        /// </summary>
        public static IServiceCollection AddPrefer(this IServiceCollection services,
            Action<PreferOptions>? configureAction = null)
        {
            if (configureAction != null)
            {
                services.AddOptions();
                services.Configure(configureAction);
            }

            services.AddScoped<PreferActionFilter>();
            services.AddMvcCore(o => { o.Filters.AddService<PreferActionFilter>(int.MaxValue); });
            return services;
        }
    }
}