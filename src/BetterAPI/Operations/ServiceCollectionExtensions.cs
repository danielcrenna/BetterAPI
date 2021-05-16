using System;
using BetterAPI.Data;
using BetterAPI.Data.Sqlite;
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

            // create a resource-based sub-system for operations
            services.TryAddSingleton(r => new SqliteResourceDataService<Operation>("operations.db", 1, r.GetRequiredService<ChangeLogBuilder>(), r.GetRequiredService<IStringLocalizer<SqliteResourceDataService<Operation>>>(), r.GetRequiredService<ILogger<SqliteResourceDataService<Operation>>>()));
            services.TryAddSingleton<IResourceDataService<Operation>>(r => r.GetRequiredService<SqliteResourceDataService<Operation>>());
            services.TryAddTransient<IResourceDataService>(r => r.GetRequiredService<SqliteResourceDataService<Operation>>());

            // create an operations host that operates against operation resources
            services.TryAddSingleton<IOperationStore>(r => new ResourceDataServiceOperationStore(
                r.GetRequiredService<IResourceDataService<Operation>>(),
                r.GetRequiredService<IStringLocalizer<ResourceDataServiceOperationStore>>(), 
                r.GetRequiredService<ILogger<ResourceDataServiceOperationStore>>()));

            return services;
        }
    }
}