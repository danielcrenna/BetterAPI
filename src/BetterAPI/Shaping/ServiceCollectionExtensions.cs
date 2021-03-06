// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BetterAPI.Shaping
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFieldInclusions(this IServiceCollection services,
            IConfiguration configuration)
        {
            return services.AddFieldInclusions(configuration.Bind);
        }

        public static IServiceCollection AddFieldInclusions(this IServiceCollection services,
            Action<IncludeOptions>? configureAction = default)
        {
            if (configureAction != default)
            {
                services.AddOptions();
                services.Configure(configureAction);
            }

            services.AddSerializerOptions();
            services.AddScoped<IncludeActionFilter>();
            services.AddMvcCore(o => { o.Filters.AddService<IncludeActionFilter>(int.MinValue); })
                .AddJsonOptions(o =>
                {
                    if(!o.JsonSerializerOptions.Converters.Any(x => x is JsonShapedDataConverterFactory))
                        o.JsonSerializerOptions.Converters.Add(new JsonShapedDataConverterFactory());
                });
            return services;
        }

        public static IServiceCollection AddFieldExclusions(this IServiceCollection services,
            IConfiguration configuration)
        {
            return services.AddFieldExclusions(configuration.Bind);
        }

        public static IServiceCollection AddFieldExclusions(this IServiceCollection services,
            Action<ExcludeOptions>? configureAction = default)
        {
            if (configureAction != default)
            {
                services.AddOptions();
                services.Configure(configureAction);
            }

            services.AddSerializerOptions();
            services.AddScoped<ExcludeActionFilter>();
            services.AddMvcCore(o => { o.Filters.AddService<ExcludeActionFilter>(int.MinValue); })
                .AddJsonOptions(o =>
                {
                    if(!o.JsonSerializerOptions.Converters.Any(x => x is JsonShapedDataConverterFactory))
                        o.JsonSerializerOptions.Converters.Add(new JsonShapedDataConverterFactory());
                });
            return services;
        }
    }
}