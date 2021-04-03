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
using System.Xml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using BetterAPI.Caching;
using BetterAPI.Cors;
using BetterAPI.DeltaQueries;
using BetterAPI.Enveloping;
using BetterAPI.Filtering;
using BetterAPI.Prefer;
using BetterAPI.Shaping;
using BetterAPI.Sorting;
using BetterAPI.Tokens;
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

            // Add core services
            services.AddTimestamps();
            services.AddSerializerOptions();
            services.AddEventServices();
            services.TryAddSingleton<TypeRegistry>();
            services.TryAddSingleton<ApiRouter>();

            // Each feature is available bespoke or bundled here by convention, and order matters:
            //
            services.AddCors(configuration.GetSection(nameof(ApiOptions.Cors)));
            services.AddTokens(configuration.GetSection(nameof(ApiOptions.Tokens)));
            services.AddDeltaQueries(configuration.GetSection(nameof(ApiOptions.DeltaQueries)));
            services.AddEnveloping();
            services.AddPrefer(configuration.GetSection(nameof(ApiOptions.Prefer)));
            services.AddHttpCaching(configuration.GetSection(nameof(ApiOptions.Cache)));
            services.AddFieldInclusions(configuration.GetSection(nameof(ApiOptions.Include)));
            services.AddFieldExclusions(configuration.GetSection(nameof(ApiOptions.Exclude)));
            // services.AddCollectionPaging(configuration.GetSection(nameof(ApiOptions.Paging));
            services.AddCollectionSorting(configuration.GetSection(nameof(ApiOptions.Sort)));
            services.AddCollectionFiltering(configuration.GetSection(nameof(ApiOptions.Filter)));
            
            var mvc = services.AddControllers()
                .AddApplicationPart(typeof(CacheController).Assembly)
                .AddXmlSupport();

            // MVC configuration with dependencies:
            services.AddSingleton<ApiGuidelinesConvention>();
            services.AddSingleton<IConfigureOptions<MvcOptions>, ConfigureMvcOptions>();
            services.AddSingleton<IConfigureOptions<ApiBehaviorOptions>, ConfigureApiBehaviorOptions>();
            services.TryAddEnumerable(ServiceDescriptor.Transient<IApplicationModelProvider, ApiGuidelinesModelProvider>());

            mvc.ConfigureApplicationPartManager(x =>
            {
                // FIXME: Implement me
                x.FeatureProviders.Add(new ApiGuidelinesControllerFeatureProvider());
            });

            mvc.AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.Converters.Add(new JsonDeltaConverterFactory());
                o.JsonSerializerOptions.Converters.Add(new JsonShapedDataConverterFactory());
            });

            // mvc.AddPolicyProtection();

            services.AddApiVersioning(
                o =>
                {
                    // reporting api versions will return the headers "api-supported-versions" and "api-deprecated-versions"
                    o.ReportApiVersions = true;

                    //options.Conventions.Controller<ValuesController>().HasApiVersion( 1, 0 );

                    //options.Conventions.Controller<Values2Controller>()
                    //    .HasApiVersion( 2, 0 )
                    //    .HasApiVersion( 3, 0 )
                    //    .Action( c => c.GetV3( default ) ).MapToApiVersion( 3, 0 )
                    //    .Action( c => c.GetV3( default, default ) ).MapToApiVersion( 3, 0 );

                    //options.Conventions.Controller<HelloWorldController>()
                    //    .HasApiVersion( 1, 0 )
                    //    .HasApiVersion( 2, 0 )
                    //    .AdvertisesApiVersion( 3, 0 );
                } );
            services.AddVersionedApiExplorer(
                o =>
                {
                    // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                    // note: the specified format code will format the version as "'v'major[.minor][-status]"
                    o.GroupNameFormat = "'v'VVV";

                    // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
                    // can also be used to control the format of the API version in route templates
                    o.SubstituteApiVersionInUrl = true;
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
                var assemblyName = assembly?.GetName().Name;
                var xmlFile = $"{assemblyName}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                o.IncludeXmlComments(xmlPath, true);
            });

            return services;
        }

        private static IMvcBuilder AddXmlSupport(this IMvcBuilder builder)
        {
            builder.AddXmlSerializerFormatters().AddXmlDataContractSerializerFormatters();
            builder.Services.AddMvc(o =>
            {
                var outputFormatter = o.OutputFormatters.OfType<XmlSerializerOutputFormatter>().First();
                outputFormatter.WriterSettings.Indent = true;
                outputFormatter.WriterSettings.NamespaceHandling = NamespaceHandling.OmitDuplicates;
            });
            return builder;
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
                options.Converters.Add(new JsonShapedDataConverterFactory());
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