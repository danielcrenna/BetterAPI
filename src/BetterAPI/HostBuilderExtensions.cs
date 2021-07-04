// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BetterAPI.Identity;
using BetterAPI.Logging;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BetterAPI
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder ConfigureApiServer<TStartup>(this IHostBuilder builder, Func<IConfiguration, IConfiguration>? configSelector = default) where TStartup : class
        {
            TStartup? startup = default;

            builder.ConfigureLogging((context, loggingBuilder) =>
            {
                loggingBuilder.AddLightingDb("logging");
            });

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

                    var startupAssembly = typeof(TStartup).Assembly;

                    // Resource assemblies:
                    //
                    foreach (var dependent in startupAssembly.GetReferencedAssemblies())
                    {
                        Assembly.Load(dependent);
                    }
                    var resourceAssemblies = new HashSet<Assembly>
                    {
                        // BetterAPI
                        typeof(ApiOptions).Assembly, 

                        // BetterAPI.Primitives
                        typeof(User).Assembly,

                        // User application
                        startupAssembly
                    };
                    foreach (var type in AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.IsDynamic)
                        .SelectMany(x => x.GetTypes()))
                    {
                        if (type.IsInterface || !typeof(IResource).IsAssignableFrom(type))
                            continue;
                        if (resourceAssemblies.Contains(type.Assembly))
                            continue;

                        resourceAssemblies.Add(type.Assembly);
                    }

                    services.AddApiServer(config, context.HostingEnvironment, resourceAssemblies);

                    //
                    // The default ApplicationPartManager relies on the entryAssemblyName, which is this library.
                    // So we need to register the calling application's own controllers as well, here.
                    var mvcBuilder = services.AddControllers();
                    foreach(var assembly in resourceAssemblies)
                        mvcBuilder.AddApplicationPart(assembly);

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
                    app.UseApiServer(context.HostingEnvironment);

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