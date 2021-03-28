using System;
using BetterAPI.DeltaQueries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BetterAPI.Deltas
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDeltaQueries(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddDeltaQueries(configuration.Bind);
        }

        public static IServiceCollection AddDeltaQueries(this IServiceCollection services, Action<DeltaQueryOptions>? configureAction = default)
        {
            if (configureAction != default)
            {
                services.AddOptions();
                services.Configure(configureAction);
            }

            services.AddScoped<DeltaQueryActionFilter>();
            services.AddMvc(o => { o.Filters.AddService<DeltaQueryActionFilter>(int.MinValue); });
            return services;
        }
    }
}
