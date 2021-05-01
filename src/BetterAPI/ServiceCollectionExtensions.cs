// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using BetterAPI.Caching;
using BetterAPI.Cors;
using BetterAPI.DeltaQueries;
using BetterAPI.Enveloping;
using BetterAPI.Filtering;
using BetterAPI.Metrics;
using BetterAPI.Paging;
using BetterAPI.Prefer;
using BetterAPI.Localization;
using BetterAPI.RateLimiting;
using BetterAPI.Shaping;
using BetterAPI.Sorting;
using BetterAPI.Tokens;
using BetterAPI.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

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
        public static IServiceCollection AddApiServer(this IServiceCollection services,
            IConfiguration configuration, Assembly? assembly = default)
        {
            assembly ??= Assembly.GetEntryAssembly();

            services.AddOptions();
            services.Configure<ApiOptions>(configuration);

            // Add core services:
            //
            services.AddTimestamps();
            services.AddEventServices();
            services.TryAddSingleton<TypeRegistry>();
            services.TryAddSingleton<ApiRouter>();
            services.AddMetrics(o =>
            {
                o.AddServerTiming();
            });
            
            // Each feature is available bespoke or bundled here by convention, and order matters:
            //
            services.AddCors(configuration.GetSection(nameof(ApiOptions.Cors)));
            services.AddApiLocalization();
            services.AddRateLimiting(configuration.GetSection(nameof(ApiOptions.RateLimiting)));
            services.AddTokens(configuration.GetSection(nameof(ApiOptions.Tokens)));
            services.AddDeltaQueries(configuration.GetSection(nameof(ApiOptions.DeltaQueries)));
            services.AddServerSidePaging(configuration.GetSection(nameof(ApiOptions.Paging)));
            services.AddEnveloping();
            services.AddPrefer(configuration.GetSection(nameof(ApiOptions.Prefer)));
            services.AddHttpCaching(configuration.GetSection(nameof(ApiOptions.Cache)));
            
            // 
            // Ensure proper order for outside-in filters:
            // See: https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#99-compound-collection-operations
            //
            services.AddFieldInclusions(configuration.GetSection(nameof(ApiOptions.Include)));
            services.AddFieldExclusions(configuration.GetSection(nameof(ApiOptions.Exclude)));
            services.AddClientSidePaging(configuration.GetSection(nameof(ApiOptions.Paging)));
            services.AddCollectionSorting(configuration.GetSection(nameof(ApiOptions.Sort)));
            services.AddCollectionFiltering(configuration.GetSection(nameof(ApiOptions.Filter)));

            services.AddVersioning(configuration.GetSection(nameof(ApiOptions.Versioning)));

            var mvc = services.AddControllers()
                .AddApplicationPart(typeof(CacheController).Assembly)
                .AddXmlSupport();

            // mvc.AddPolicyProtection();

            // MVC configuration with dependencies:
            //
            services.AddSingleton<ApiGuidelinesConvention>();
            services.AddSingleton<IConfigureOptions<MvcOptions>, ConfigureMvcOptions>();
            services.AddSingleton<IConfigureOptions<ApiBehaviorOptions>, ConfigureApiBehaviorOptions>();
            services.TryAddEnumerable(ServiceDescriptor.Transient<IApplicationModelProvider, ApiGuidelinesModelProvider>());

            mvc.ConfigureApplicationPartManager(x =>
            {
                // FIXME: Implement me
                x.FeatureProviders.Add(new ApiGuidelinesControllerFeatureProvider());
            });

            services.AddSingleton<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerGenOptions>();
            services.AddSwaggerGen(o =>
            {
                o.OperationFilter<VersioningOperationFilter>();
                o.DocumentFilter<DocumentationDocumentFilter>();
                o.OperationFilter<DocumentationOperationFilter>();
                o.SchemaFilter<DocumentationSchemaFilter>();

                // Set the comments path for the Swagger JSON and UI.
                // See: https://docs.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-5.0&tabs=visual-studio#xml-comments
                //
                var assemblyName = assembly?.GetName().Name;
                var xmlFile = $"{assemblyName}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if(File.Exists(xmlPath))
                    o.IncludeXmlComments(xmlPath, true);
            });

            return services;
        }

        private static IMvcBuilder AddXmlSupport(this IMvcBuilder builder)
        {
            builder.AddXmlSerializerFormatters().AddXmlDataContractSerializerFormatters();
            builder.Services.AddMvcCore(o =>
            {
                var outputFormatter = o.OutputFormatters.OfType<XmlSerializerOutputFormatter>().First();
                outputFormatter.WriterSettings.Indent = true;
                outputFormatter.WriterSettings.NamespaceHandling = NamespaceHandling.OmitDuplicates;
            });
            return builder;
        }

        /// <summary>
        ///     Adds timestamp generation for services that require them. The default implementation is the local server's wall time.
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
            services.AddMvcCore()
                .AddJsonOptions(o =>
                {
                    // 
                    // Currently, we're setting the enum to camelCase because it's indicated in the guidelines
                    // (though it's shown once as PascalCase, so it's not entirely clear):
                    // https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#1323-post-hybrid-model
                    if(!o.JsonSerializerOptions.Converters.Any(x => x is JsonStringEnumConverter))
                        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                });

            return services;
        }

        public static IServiceCollection AddEventServices(this IServiceCollection services)
        {
            services.TryAddSingleton<IEventBroadcaster, DefaultEventBroadcaster>();
            return services;
        }

        public static ChangeLogBuilder AddApiResource(this IServiceCollection services, string resourceName, Action<ChangeLogBuilder>? builderAction = default)
        {
            var builder = new ChangeLogBuilder(resourceName, services);
            builderAction?.Invoke(builder);
            return builder;
        }
    }
}