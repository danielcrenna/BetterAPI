using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BetterAPI.Cors
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///     Adds CORS support per the Microsoft REST Guidelines.
        ///     <see href=" https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#8-cors/" />
        /// </summary>
        public static IServiceCollection AddCors(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddCors(configuration.Bind);
        }

        /// <summary>
        ///     Adds CORS support per the Microsoft REST Guidelines.
        ///     <see href=" https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#8-cors/" />
        /// </summary>
        public static IServiceCollection AddCors(this IServiceCollection services, Action<CorsOptions>? configureAction = default)
        {
            if (configureAction != default)
            {
                services.AddOptions();
                services.Configure(configureAction);
            }

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

            return services;
        }
    }
}
