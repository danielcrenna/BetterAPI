using Microsoft.Extensions.DependencyInjection;

namespace BetterAPI.Operations
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLongRunningOperations(this IServiceCollection services)
        {
            return services;
        }
    }
}