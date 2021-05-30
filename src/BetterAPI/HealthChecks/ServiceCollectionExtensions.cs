using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BetterAPI.HealthChecks
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiHealthChecks(this IServiceCollection services)
        {
            services.AddHealthChecks();
            services.TryAddSingleton(services);
            services.TryAddEnumerable(ServiceDescriptor.Transient<IStartupFilter, HealthCheckStartupFilter>());
            
            var builder = services.AddHealthChecks();
            builder.AddCheck<ServicesHealthCheck>(nameof(ServicesHealthCheck), HealthStatus.Unhealthy, new[] {"startup"});
            return services;
        }
    }
}
