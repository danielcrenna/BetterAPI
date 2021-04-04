using System;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BetterAPI.Versioning
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddVersioning(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddVersioning(configuration.Bind);
        }

        public static IServiceCollection AddVersioning(this IServiceCollection services, Action<VersioningOptions>? configureAction = default)
        {
            if (configureAction != default)
            {
                services.AddOptions();
                services.Configure(configureAction);
            }

            services.AddSingleton<IConfigureOptions<ApiVersioningOptions>, ConfigureApiVersioning>();
            services.AddApiVersioning();
            services.AddVersionedApiExplorer(
                o =>
                {
                    // Add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                    // Note: the specified format code will format the version as "'v'major[.minor][-status]"
                    o.GroupNameFormat = "'v'VVV";

                    // note: this option is only necessary when versioning by url segment.
                    // The SubstitutionFormat can also be used to control the format of the API version in route templates
                    o.SubstituteApiVersionInUrl = true;
                });

            return services;
        }
    }
}
