using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BetterAPI.Search
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSearch(this IServiceCollection services,
            IConfiguration configuration)
        {
            return services.AddSearch(configuration.Bind);
        }

        public static IServiceCollection AddSearch(this IServiceCollection services,
            Action<SearchOptions>? configureAction = default)
        {
            if (configureAction != default)
            {
                services.AddOptions();
                services.Configure(configureAction);
            }

            services.AddSerializerOptions();
            services.TryAddScoped<SearchActionFilter>();
            services.AddMvcCore(o => { o.Filters.AddService<SearchActionFilter>(int.MinValue); })
                .AddJsonOptions(o =>
                {
                    
                });
            return services;
        }
    }
}
