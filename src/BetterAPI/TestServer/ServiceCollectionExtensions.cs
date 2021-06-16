using BetterAPI.Events;
using BetterAPI.Http.Interception;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BetterAPI.TestServer
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTestCollector(this IServiceCollection services)
        {
            services.AddRequestInterception(o => o.Enabled = true);
            services.TryAddSingleton<ISnapshotStore, FileSnapshotStore>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IRequestEventBroadcaster, SnapshotRequestEventBroadcaster>());
            return services;
        }
    }
}
