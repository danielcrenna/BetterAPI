using System;
using System.Text.Json;
using BetterApi.Guidelines;
using BetterApi.Guidelines.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BetterAPI.Guidelines.Caching
{
    public static class Add
    {
        public static IServiceCollection AddInProcessCache(this IServiceCollection services,
            Action<CacheOptions> configureAction = null)
        {
            services.AddOptions();

            if (configureAction != null)
                services.Configure(configureAction);

            services.TryAdd(ServiceDescriptor.Singleton<Func<DateTimeOffset>>(r => () => DateTimeOffset.Now));
            services.TryAdd(ServiceDescriptor.Singleton<IMemoryCache, MemoryCache>());
            services.TryAdd(ServiceDescriptor.Singleton<ICache, InProcessCache>());

            return services;
        }

        public static IServiceCollection AddDistributedCache(this IServiceCollection services,
            Action<CacheOptions> configureAction = null)
        {
            services.AddOptions();

            if (configureAction != null)
                services.Configure(configureAction);

            services.TryAdd(ServiceDescriptor.Singleton<Func<DateTimeOffset>>(r => () => DateTimeOffset.Now));
            services.TryAdd(ServiceDescriptor.Singleton<IDistributedCache, MemoryDistributedCache>());
            services.TryAdd(ServiceDescriptor.Singleton<ICache, DistributedCache>());

            return services;
        }

        public static IServiceCollection AddHttpCaching(this IServiceCollection services)
        {
            services.TryAddSingleton<IHttpCache, InProcessHttpCache>();
            services.TryAddSingleton(r => new JsonSerializerOptions(JsonSerializerDefaults.Web));
            services.AddScoped(r => new HttpCacheFilterAttribute(r.GetRequiredService<IHttpCache>(), r.GetRequiredService<JsonSerializerOptions>()));
            var mvc = services.AddMvc(o =>
            {
                o.Filters.AddService<HttpCacheFilterAttribute>();
            });
            return services;
        }
    }
}
