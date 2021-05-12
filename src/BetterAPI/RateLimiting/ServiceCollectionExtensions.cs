using System;
using BetterAPI.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BetterAPI.RateLimiting
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRateLimiting(this IServiceCollection services,
            IConfiguration configuration)
        {
            return services.AddRateLimiting(configuration.Bind);
        }

        public static IServiceCollection AddRateLimiting(this IServiceCollection services,
            Action<RateLimitingOptions>? configureAction = default)
        {
            if (configureAction != default)
            {
                services.AddOptions();
                services.Configure(configureAction);
            }

            services.AddMetrics(builder =>
            {
                // See: https://docs.microsoft.com/en-us/dotnet/core/diagnostics/event-counters
                // See: https://docs.microsoft.com/en-us/dotnet/core/diagnostics/available-counters

                builder.RegisterEventSourceCounter("Microsoft.AspNetCore.Hosting", "current-requests");
                builder.RegisterEventSourceCounter("Microsoft.AspNetCore.Hosting", "failed-requests");
                builder.RegisterEventSourceCounter("Microsoft.AspNetCore.Hosting", "requests-per-second");
                builder.RegisterEventSourceCounter("Microsoft.AspNetCore.Hosting", "total-requests");
            });

            services.AddScoped<RateLimitingActionFilter>();
            services.AddMvcCore(o => { o.Filters.AddService<RateLimitingActionFilter>(int.MaxValue); });

            return services;
        }
    }
}
