using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BetterAPI.Paging
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCollectionPaging(this IServiceCollection services, IConfiguration configuration) => services.AddCollectionPaging(configuration.Bind);

        public static IServiceCollection AddCollectionPaging(this IServiceCollection services, Action<PagingOptions>? configureAction = default)
        {
            if (configureAction != default)
            {
                services.AddOptions();
                services.Configure(configureAction);
            }

            services.AddTopPaging(options =>
            {
                var paging = new PagingOptions();
                configureAction?.Invoke(paging);
                options.HasDefaultBehaviorWhenMissing = paging.Top.HasDefaultBehaviorWhenMissing;
                options.Operator = paging.Top.Operator;
            });

            services.AddSkipPaging(options =>
            {
                var paging = new PagingOptions();
                configureAction?.Invoke(paging);
                options.HasDefaultBehaviorWhenMissing = paging.Skip.HasDefaultBehaviorWhenMissing;
                options.Operator = paging.Skip.Operator;
            });

            return services;
        }

        public static IServiceCollection AddTopPaging(this IServiceCollection services, IConfiguration configuration) => services.AddTopPaging(configuration.Bind);

        public static IServiceCollection AddTopPaging(this IServiceCollection services, Action<TopOptions>? configureAction = default)
        {
            if (configureAction != default)
            {
                services.AddOptions();
                services.Configure(configureAction);
            }

            services.TryAddScoped<TopActionFilter>();
            services.AddMvc(o => { o.Filters.AddService<TopActionFilter>(int.MinValue); });
            return services;
        }

        public static IServiceCollection AddSkipPaging(this IServiceCollection services, IConfiguration configuration) => services.AddSkipPaging(configuration.Bind);

        public static IServiceCollection AddSkipPaging(this IServiceCollection services, Action<SkipOptions>? configureAction = default)
        {
            if (configureAction != default)
            {
                services.AddOptions();
                services.Configure(configureAction);
            }
            
            services.TryAddScoped<SkipActionFilter>();
            services.AddMvc(o => { o.Filters.AddService<SkipActionFilter>(int.MinValue); });
            return services;
        }
    }
}
