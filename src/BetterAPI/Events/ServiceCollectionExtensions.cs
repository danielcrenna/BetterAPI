using BetterAPI.Http.Interception;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BetterAPI.Events
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEventServices(this IServiceCollection services)
        {
            services.AddRequestInterception(o => o.Enabled = true);
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IResourceEventBroadcaster, DeltaQueryResourceEventBroadcaster>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IRequestEventBroadcaster, LogRequestEventBroadcaster>());
            return services;
        }
    }
}
