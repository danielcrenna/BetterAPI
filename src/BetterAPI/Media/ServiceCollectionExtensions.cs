using Microsoft.Extensions.DependencyInjection;

namespace BetterAPI.Media
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMediaServer(this IServiceCollection services)
        {
            return services;
        }
    }
}
