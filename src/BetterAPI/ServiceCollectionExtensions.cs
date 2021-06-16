// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using BetterAPI.Caching;
using BetterAPI.ChangeLog;
using BetterAPI.DataProtection;
using BetterAPI.DeltaQueries;
using BetterAPI.Enveloping;
using BetterAPI.Events;
using BetterAPI.Filtering;
using BetterAPI.Guidelines.Cors;
using BetterAPI.HealthChecks;
using BetterAPI.Identity;
using BetterAPI.Metrics;
using BetterAPI.Paging;
using BetterAPI.Prefer;
using BetterAPI.Localization;
using BetterAPI.OpenApi;
using BetterAPI.Operations;
using BetterAPI.Patch;
using BetterAPI.RateLimiting;
using BetterAPI.Search;
using BetterAPI.Shaping;
using BetterAPI.Sorting;
using BetterAPI.Tokens;
using BetterAPI.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Formatters;
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
        public static IServiceCollection AddApiServer(this IServiceCollection services,
            IConfiguration configuration, Assembly? assembly = default)
        {
            assembly ??= Assembly.GetEntryAssembly();

            services.AddOptions();
            services.Configure<ApiOptions>(configuration);

            // Core services:
            //
            services.AddTimestamps();
            services.TryAddSingleton(assembly == default ? new ResourceTypeRegistry() : new ResourceTypeRegistry(assembly));
            services.TryAddSingleton<ApiRouter>();
            services.AddMetrics(o =>
            {
                o.AddServerTiming();
            });
            services.AddApiHealthChecks();

            // Background services:
            //
            services.AddApiLocalization();
            services.AddLongRunningOperations();
            services.AddEventServices();
            services.AddApiIdentity();

            // Each feature is available bespoke or bundled here by convention, and order matters:
            //
            services.AddGuidelinesCors(configuration.GetSection(nameof(ApiOptions.Cors)));
            services.AddRateLimiting(configuration.GetSection(nameof(ApiOptions.RateLimiting)));
            services.AddTokens(configuration.GetSection(nameof(ApiOptions.Tokens)));
            services.AddDeltaQueries(configuration.GetSection(nameof(ApiOptions.DeltaQueries)));
            services.AddServerSidePaging(configuration.GetSection(nameof(ApiOptions.Paging)));
            services.AddEnveloping();
            services.AddPrefer(configuration.GetSection(nameof(ApiOptions.Prefer)));
            services.AddHttpCaching(configuration.GetSection(nameof(ApiOptions.Cache)));
            
            // Ensure proper order for outside-in filters:
            // See: https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#99-compound-collection-operations
            //
            services.AddFieldInclusions(configuration.GetSection(nameof(ApiOptions.Include)));
            services.AddFieldExclusions(configuration.GetSection(nameof(ApiOptions.Exclude)));
            services.AddClientSidePaging(configuration.GetSection(nameof(ApiOptions.Paging)));
            services.AddCollectionSorting(configuration.GetSection(nameof(ApiOptions.Sort)));
            services.AddCollectionFiltering(configuration.GetSection(nameof(ApiOptions.Filter)));
            services.AddSearch(configuration.GetSection(nameof(ApiOptions.Search)));
            services.AddVersioning(configuration.GetSection(nameof(ApiOptions.Versioning)));
            
            // Canonicalize with lowercase paths
            //
            services.AddRouting(o =>
            {
                o.AppendTrailingSlash = true;
                o.LowercaseQueryStrings = false;
                o.LowercaseUrls = true;
            });

            var mvc = services.AddControllers(o =>
            {
                var inputFormatter = o.InputFormatters.OfType<SystemTextJsonInputFormatter>().First();
                inputFormatter.SupportedMediaTypes.Add(ApiMediaTypeNames.Application.JsonMergePatch);

                var outputFormatter = o.OutputFormatters.OfType<SystemTextJsonOutputFormatter>().First();
                outputFormatter.SupportedMediaTypes.Add(ApiMediaTypeNames.Application.JsonMergePatch);
            })
            .AddControllersAsServices() // necessary to enable embedded collection aliased method
            .AddApplicationPart(typeof(CacheController).Assembly)
            .AddXmlSupport();

            services.AddPolicyProtection();

            // MVC configuration with dependencies:
            //
            services.AddSingleton<ApiGuidelinesConvention>();
            services.AddSingleton<IConfigureOptions<MvcOptions>, ConfigureMvcOptions>();
            services.AddSingleton<IConfigureOptions<ApiBehaviorOptions>, ConfigureApiBehaviorOptions>();
            services.TryAddEnumerable(ServiceDescriptor.Transient<IApplicationModelProvider, ApiGuidelinesModelProvider>());

            mvc.ConfigureApplicationPartManager(x =>
            {
                x.FeatureProviders.Add(new ApiGuidelinesControllerFeatureProvider(services.GetChangeLog()));
            });

            services.AddOpenApi(assembly);

            return services;
        }

        private static IMvcBuilder AddXmlSupport(this IMvcBuilder builder)
        {
            builder.AddXmlDataContractSerializerFormatters();
            builder.Services.AddMvcCore(o =>
            {
                var outputFormatter = o.OutputFormatters.OfType<XmlDataContractSerializerOutputFormatter>().First();
                outputFormatter.SupportedMediaTypes.Add(ApiMediaTypeNames.Application.XmlMergePatch);
                outputFormatter.WriterSettings.Indent = true;
                outputFormatter.WriterSettings.NamespaceHandling = NamespaceHandling.OmitDuplicates;
            });
            return builder;
        }

        public static IServiceCollection AddSerializerOptions(this IServiceCollection services)
        {
            services.AddMvcCore()
                .AddJsonOptions(o =>
                {
                    if(!o.JsonSerializerOptions.Converters.Any(x => x is JsonMergePatchConverterFactory))
                        o.JsonSerializerOptions.Converters.Add(new JsonMergePatchConverterFactory());

                    // 
                    // Currently, we're setting the enum to camelCase because it's indicated in the guidelines
                    // (though it's shown once as PascalCase, so it's not entirely clear):
                    // https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#1323-post-hybrid-model
                    if(!o.JsonSerializerOptions.Converters.Any(x => x is JsonStringEnumConverter))
                        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                });

            return services;
        }

        
    }
}