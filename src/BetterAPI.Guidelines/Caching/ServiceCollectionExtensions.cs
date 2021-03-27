using System;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BetterAPI.Guidelines.Caching
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInProcessCache(this IServiceCollection services, Action<CacheOptions>? configureAction = null)
        {
            services.AddOptions();

            if (configureAction != null)
                services.Configure(configureAction);

            services.AddTimestamps();
            services.TryAdd(ServiceDescriptor.Singleton<IMemoryCache, MemoryCache>());
            services.TryAdd(ServiceDescriptor.Singleton<ICache, InProcessCache>());

            return services;
        }

        public static IServiceCollection AddDistributedCache(this IServiceCollection services, Action<CacheOptions>? configureAction = null)
        {
            services.AddOptions();

            if (configureAction != null)
                services.Configure(configureAction);

            services.AddTimestamps();
            services.TryAdd(ServiceDescriptor.Singleton<IDistributedCache, MemoryDistributedCache>());
            services.TryAdd(ServiceDescriptor.Singleton<ICache, DistributedCache>());

            return services;
        }

        public static IServiceCollection AddHttpCaching(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddHttpCaching(configuration.Bind);
        }

        public static IServiceCollection AddHttpCaching(this IServiceCollection services, Action<CacheOptions>? configureAction = null)
        {
            if (configureAction != null)
                services.Configure(configureAction);

            services.TryAddSingleton<InProcessHttpCache>();
            services.TryAddSingleton<IHttpCache>(r => r.GetRequiredService<InProcessHttpCache>());
            services.TryAddSingleton<ICacheManager>(r => r.GetRequiredService<InProcessHttpCache>());

            services.TryAddSingleton(r => new JsonSerializerOptions(JsonSerializerDefaults.Web));
            services.AddScoped(r => new HttpCacheActionFilter(r.GetRequiredService<IHttpCache>(), r.GetRequiredService<JsonSerializerOptions>()));
            services.AddMvc(o =>
            {
                o.Filters.AddService<HttpCacheActionFilter>(int.MinValue);
            });
            return services;
        }
    }
}
