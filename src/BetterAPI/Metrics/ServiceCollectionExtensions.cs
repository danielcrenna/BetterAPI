// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace BetterAPI.Metrics
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMetrics(this IServiceCollection services)
        {
            return AddMetrics(services, builder => { });
        }

        public static IServiceCollection AddMetrics(this IServiceCollection services, IConfiguration configuration)
        {
            return AddMetrics(services, configuration.Bind);
        }

        public static IServiceCollection AddMetrics(this IServiceCollection services, IConfiguration configuration,
            Action<MetricsBuilder> configure)
        {
            return AddMetrics(services, builder =>
            {
                var options = new MetricsOptions();
                configuration.Bind(options);
                configure(builder);
            });
        }

        public static IServiceCollection AddMetrics(this IServiceCollection services, Action<MetricsBuilder> configure)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddOptions();

            services.AddSingleton<IMetricsBuilder, MetricsBuilder>(r =>
            {
                var builder = new MetricsBuilder(services, services.AddHealthChecks());
                configure(builder);
                return builder;
            });

            services.TryAddSingleton<IMetricsStore, MemoryMetricsStore>();
            services.TryAddSingleton<IMetricsHost>(r =>
            {
                var host = new MetricsHost(r.GetRequiredService<IMetricsStore>());
                return host;
            });
            services.TryAddSingleton<IMetricsRegistry>(r =>
            {
                var registry = new MemoryMetricsRegistry
                {
                    r.GetRequiredService<IMetricsHost>()
                };
                return registry;
            });

            services.TryAdd(ServiceDescriptor.Singleton(typeof(IMetricsHost<>), typeof(MetricsHost<>)));
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, EventSourceHostedService>());

            return services;
        }

        public static MetricsBuilder AddServerTiming(this MetricsBuilder builder,
            Action<ServerTimingReporterOptions>? configureAction = default)
        {
            if (configureAction != default)
            {
                builder.Services.AddOptions();
                builder.Services.Configure(configureAction);
            }

            return builder;
        }
    }
}