using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BetterAPI.Caching
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCacheRegions(this IServiceCollection services)
        {
            services.TryAdd(ServiceDescriptor.Singleton(typeof(ICacheRegion<>), typeof(CacheRegion<>)));
            return services;
        }
    }
}
