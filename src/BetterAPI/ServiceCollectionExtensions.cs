// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using BetterAPI.Caching;
using BetterAPI.Cors;
using BetterAPI.DataProtection;
using BetterAPI.DeltaQueries;
using BetterAPI.Enveloping;
using BetterAPI.Filtering;
using BetterAPI.Prefer;
using BetterAPI.Sorting;
using BetterAPI.Tokens;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;

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
            services.AddSerializerOptions();
            services.AddEventServices();

            // Each feature is available bespoke or bundled here by convention:
            services.AddCors(configuration.GetSection(nameof(ApiOptions.Cors)));
            services.AddTokens(configuration.GetSection(nameof(ApiOptions.Tokens)));
            services.AddDeltaQueries(configuration.GetSection(nameof(ApiOptions.DeltaQueries)));
            services.AddEnveloping();
            services.AddPrefer(configuration.GetSection(nameof(ApiOptions.Prefer)));
            services.AddHttpCaching(configuration.GetSection(nameof(ApiOptions.Cache)));
            services.AddCollectionSorting(configuration.GetSection(nameof(ApiOptions.Sort)));
            services.AddCollectionFiltering(configuration.GetSection(nameof(ApiOptions.Filter)));

            var mvc = services.AddControllers();
            
            // MVC configuration with dependencies:
            services.AddSingleton<IConfigureOptions<ApiBehaviorOptions>, ConfigureApiBehaviorOptions>();
            services.AddSingleton<ApiGuidelinesConvention>();
            services.AddSingleton<IConfigureOptions<MvcOptions>, ConfigureMvcOptions>();
            
            mvc.ConfigureApplicationPartManager(x =>
            {
                // FIXME: Implement me
                x.FeatureProviders.Add(new ApiGuidelinesControllerFeatureProvider());
            });

            mvc.AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.Converters.Add(new JsonDeltaConverterFactory());
            });

            // mvc.AddPolicyProtection();
            
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
                c.IncludeXmlComments(xmlPath, true);

                c.DocumentFilter<DocumentationDocumentFilter>();
                c.OperationFilter<DocumentationOperationFilter>();
            });

            return services;
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

        public static IServiceCollection AddSerializerOptions(this IServiceCollection services)
        {
            services.TryAddSingleton(r =>
            {
                var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                options.Converters.Add(new JsonDeltaConverterFactory());
                return options;
            });
            return services;
        }

        public static IServiceCollection AddEventServices(this IServiceCollection services)
        {
            services.TryAddSingleton<IEventBroadcaster, DefaultEventBroadcaster>();
            return services;
        }
    }
}