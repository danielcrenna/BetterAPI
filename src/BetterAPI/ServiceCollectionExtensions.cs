// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Reflection;
using BetterAPI.Caching;
using BetterAPI.Enveloping;
using BetterAPI.Prefer;
using BetterAPI.Sorting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;

namespace BetterAPI
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///     Adds all Microsoft REST API Guidelines as pre-configured middleware.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static IServiceCollection AddApiGuidelines(this IServiceCollection services,
            IConfiguration configuration, Assembly? assembly = default)
        {
            assembly ??= Assembly.GetEntryAssembly();

            services.AddOptions();
            services.Configure<ApiOptions>(configuration);

            // Add core services
            services.AddTimestamps();

            // Each feature is available bespoke or bundled here by convention:
            services.AddCors();
            services.AddEnveloping();
            services.AddPrefer(configuration.GetSection(nameof(ApiOptions.Prefer)));
            services.AddHttpCaching(configuration.GetSection(nameof(ApiOptions.Cache)));
            services.AddCollectionSorting(configuration.GetSection(nameof(ApiOptions.Sort)));

            services.AddControllers()
                // See: https://docs.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-5.0#use-apibehavioroptionsclienterrormapping
                .ConfigureApiBehaviorOptions(o =>
                {
                    o.ClientErrorMapping[400] = new ClientErrorData
                    {
                        Title = "Bad Request",
                        Link = "https://httpstatuscodes.com/400"
                    };
                });

            // FIXME: Implement me
            services.AddControllers().ConfigureApplicationPartManager(x =>
            {
                x.FeatureProviders.Add(new ApiGuidelinesControllerFeatureProvider());
            });

            services.AddSwaggerGen(c =>
            {
                var settings = configuration.Get<ApiOptions>() ?? new ApiOptions();

                var info = new OpenApiInfo {Title = settings.ApiName, Version = settings.ApiVersion};

                c.SwaggerDoc("v1", info);

                // Set the comments path for the Swagger JSON and UI.
                // See: https://docs.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-5.0&tabs=visual-studio#xml-comments
                var assemblyName = assembly?.GetName().Name;
                var xmlFile = $"{assemblyName}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);

                c.OperationFilter<DocumentationOperationFilter>();
            });

            return services;
        }

        /// <summary>
        ///     Adds CORS support per the Microsoft REST Guidelines.
        ///     <see href=" https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#8-cors/" />
        /// </summary>
        /// <param name="services"></param>
        public static void AddCors(this IServiceCollection services)
        {
            // Services compliant with the Microsoft REST API Guidelines MUST support CORS (Cross Origin Resource Sharing).
            // Services SHOULD support an allowed origin of CORS * and enforce authorization through valid OAuth tokens.
            // Services SHOULD NOT support user credentials with origin validation. There MAY be exceptions for special cases.

            services.AddCors(o =>
            {
                o.AddDefaultPolicy(x =>
                {
                    x.AllowAnyOrigin();
                    x.AllowAnyHeader();
                    x.AllowAnyMethod();
                    x.DisallowCredentials();
                });
            });
        }

        /// <summary>
        ///     Adds timestamp generation for services that require them. The default implementation is the local server wall time.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddTimestamps(this IServiceCollection services)
        {
            services.TryAdd(ServiceDescriptor.Singleton<Func<DateTimeOffset>>(r => () => DateTimeOffset.Now));
            return services;
        }
    }
}