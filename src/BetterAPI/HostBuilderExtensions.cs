// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BetterAPI
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder HostApiServer<TStartup>(this IHostBuilder builder,
            Func<IConfiguration, IConfiguration>? configSelector = default) where TStartup : class
        {
            TStartup? startup = default;

            builder.ConfigureAppConfiguration((context, configBuilder) => { });

            builder.ConfigureWebHostDefaults(configure =>
            {
                configure.UseKestrel(o =>
                {
                    o.AddServerHeader = false; // we add our own header via configuration
                });

                configure.UseStartup<TStartup>(); // only called for method validation
                configure.UseStaticWebAssets();

                configure.ConfigureServices((context, services) =>
                {
                    var config = configSelector != default
                        ? configSelector(context.Configuration)
                        : context.Configuration.GetSection(Constants.DefaultConfigSection);

                    services.AddApiGuidelines(config, typeof(TStartup).Assembly);

                    //
                    // The default ApplicationPartManager relies on the entryAssemblyName, which is this library.
                    services.AddControllers().AddApplicationPart(typeof(TStartup).Assembly);

                    //
                    // Calling `configure.UseStartup<TStartup>()` will replace our defaults, so call manually:
                    var serviceProvider = services.BuildServiceProvider();
                    startup ??= Instancing.CreateInstance<TStartup>(serviceProvider);
                    var accessor = CallAccessor.Create(typeof(TStartup).GetMethod(nameof(IStartup.ConfigureServices)) ??
                                                       throw new InvalidOperationException());
                    accessor.Call(startup, new object[] {services});
                });

                configure.Configure((context, app) =>
                {
                    app.UseApiServer();

                    //
                    // Calling `configure.UseStartup<TStartup>()` will replace our defaults, so call manually:
                    startup ??= Instancing.CreateInstance<TStartup>(app.ApplicationServices);
                    var accessor = CallAccessor.Create(typeof(TStartup).GetMethod(nameof(IStartup.Configure)) ??
                                                       throw new InvalidOperationException());
                    accessor.Call(startup, new AppBuilderServiceProvider(app, app.ApplicationServices));
                });
            });

            return builder;
        }
    }
}