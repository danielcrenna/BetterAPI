using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BetterAPI.Paging
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServerSidePaging(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddServerSidePaging(configuration.Bind);
        }

        public static IServiceCollection AddServerSidePaging(this IServiceCollection services, Action<PagingOptions>? configureAction = default)
        {
            if (configureAction != default)
            {
                services.AddOptions();
                services.Configure(configureAction);
            }

            services.AddSerializerOptions();
            services.TryAddSingleton<IPageQueryStore, DefaultPageQueryStore>();
            services.TryAddScoped<PagingActionFilter>();
            services.AddMvcCore(o => { o.Filters.AddService<PagingActionFilter>(int.MinValue); })
                .AddJsonOptions(o =>
                {
                    if(!o.JsonSerializerOptions.Converters.Any(x => x is JsonNextLinkConverterFactory))
                        o.JsonSerializerOptions.Converters.Add(new JsonNextLinkConverterFactory());
                });
            return services;
        }
        
        public static IServiceCollection AddClientSidePaging(this IServiceCollection services, IConfiguration configuration) => services.AddClientSidePaging(configuration.Bind);

        public static IServiceCollection AddClientSidePaging(this IServiceCollection services, Action<PagingOptions>? configureAction = default)
        {
            if (configureAction != default)
            {
                services.AddOptions();
                services.Configure(configureAction);
            }
            
            services.AddSkipPaging(options =>
            {
                var paging = new PagingOptions();
                configureAction?.Invoke(paging);
                options.HasDefaultBehaviorWhenMissing = paging.Skip.HasDefaultBehaviorWhenMissing;
                options.Operator = paging.Skip.Operator;
            });

            services.AddTopPaging(options =>
            {
                var paging = new PagingOptions();
                configureAction?.Invoke(paging);
                options.HasDefaultBehaviorWhenMissing = paging.Top.HasDefaultBehaviorWhenMissing;
                options.Operator = paging.Top.Operator;
            });

            services.AddMaxPageSizePaging(options =>
            {
                var paging = new PagingOptions();
                configureAction?.Invoke(paging);
                options.HasDefaultBehaviorWhenMissing = paging.MaxPageSize.HasDefaultBehaviorWhenMissing;
                options.Operator = paging.MaxPageSize.Operator;
                options.DefaultPageSize = paging.MaxPageSize.DefaultPageSize;
            });

            services.AddCountPaging(options =>
            {
                var paging = new PagingOptions();
                configureAction?.Invoke(paging);
                options.HasDefaultBehaviorWhenMissing = paging.Count.HasDefaultBehaviorWhenMissing;
                options.Operator = paging.Count.Operator;
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
            services.AddMvcCore(o => { o.Filters.AddService<TopActionFilter>(int.MinValue); });
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
            services.AddMvcCore(o => { o.Filters.AddService<SkipActionFilter>(int.MinValue); });
            return services;
        }

        public static IServiceCollection AddMaxPageSizePaging(this IServiceCollection services, IConfiguration configuration) => services.AddMaxPageSizePaging(configuration.Bind);

        public static IServiceCollection AddMaxPageSizePaging(this IServiceCollection services, Action<MaxPageSizeOptions>? configureAction = default)
        {
            if (configureAction != default)
            {
                services.AddOptions();
                services.Configure(configureAction);
            }

            services.TryAddScoped<MaxPageSizeActionFilter>();
            services.AddMvcCore(o => { o.Filters.AddService<MaxPageSizeActionFilter>(int.MinValue); });
            return services;
        }

        public static IServiceCollection AddCountPaging(this IServiceCollection services, IConfiguration configuration) => services.AddCountPaging(configuration.Bind);

        public static IServiceCollection AddCountPaging(this IServiceCollection services, Action<CountOptions>? configureAction = default)
        {
            if (configureAction != default)
            {
                services.AddOptions();
                services.Configure(configureAction);
            }

            services.TryAddScoped<CountActionFilter>();
            services.AddMvcCore(o => { o.Filters.AddService<CountActionFilter>(int.MinValue); });
            return services;
        }
    }
}
