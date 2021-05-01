// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BetterAPI.DeltaQueries
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDeltaQueries(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddDeltaQueries(configuration.Bind);
        }

        public static IServiceCollection AddDeltaQueries(this IServiceCollection services, Action<DeltaQueryOptions>? configureAction = default)
        {
            if (configureAction != default)
            {
                services.AddOptions();
                services.Configure(configureAction);
            }

            services.AddSerializerOptions();
            services.TryAddSingleton<IDeltaQueryStore, DefaultDeltaQueryStore>();
            services.TryAddScoped<DeltaQueryActionFilter>();
            services.AddMvcCore(o =>
                {
                    o.Filters.AddService<DeltaQueryActionFilter>(int.MinValue);
                }).AddJsonOptions(o =>
                {
                    if(!o.JsonSerializerOptions.Converters.Any(x => x is JsonDeltaConverterFactory))
                        o.JsonSerializerOptions.Converters.Add(new JsonDeltaConverterFactory());
                });
            return services;
        }
    }
}