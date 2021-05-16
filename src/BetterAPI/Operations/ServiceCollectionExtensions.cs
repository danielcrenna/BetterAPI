using System;
using BetterAPI.Filtering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace BetterAPI.Operations
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLongRunningOperations(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddLongRunningOperations(configuration.Bind);
        }

        public static IServiceCollection AddLongRunningOperations(this IServiceCollection services, Action<OperationOptions>? configureAction = default)
        {
            if (configureAction != default)
            {
                services.AddOptions();
                services.Configure(configureAction);
            }

            services.TryAddSingleton<OperationsHost>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IHostedService), typeof(OperationsHostedService)));
            services.TryAddSingleton<IOperationStore>(r => new SqliteOperationStore("operations.db", r.GetRequiredService<IStringLocalizer<SqliteOperationStore>>(), r.GetRequiredService<ILogger<SqliteOperationStore>>()));
            return services;
        }
    }
}