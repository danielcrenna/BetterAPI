using System;
using System.IO;
using System.Reflection;
using BetterAPI.Guidelines.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace BetterAPI.Guidelines
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiGuidelines(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();
            services.Configure<ApiOptions>(configuration);

            services.AddSingleton<Func<DateTimeOffset>>(r => () => DateTimeOffset.Now);
            services.AddHttpCaching();

            services.AddControllers()
                // See: https://docs.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-5.0#use-apibehavioroptionsclienterrormapping
                .ConfigureApiBehaviorOptions(o => { });
            
            services.AddSingleton<ApiGuidelinesActionFilter>();

            var mvc = services.AddMvc(o =>
            {
                o.Filters.AddService<ApiGuidelinesActionFilter>(int.MaxValue);
            });

            mvc.ConfigureApplicationPartManager(x =>
            {
                x.FeatureProviders.Add(new ApiGuidelinesControllerFeatureProvider());
            });

            services.AddSwaggerGen(c =>
            {
                var settings = configuration.Get<ApiOptions>() ?? new ApiOptions();

                var info = new OpenApiInfo {Title = settings.ApiName, Version = settings.ApiVersion};

                c.SwaggerDoc("v1", info);

                // Set the comments path for the Swagger JSON and UI.
                // See: https://docs.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-5.0&tabs=visual-studio#xml-comments
                var xmlFile = $"{Assembly.GetEntryAssembly()?.GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);

                c.OperationFilter<ApiGuidelinesOperationFilter>();
            });

            return services;
        }
    }
}