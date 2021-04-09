using System;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

namespace BetterAPI.Localization
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLocalization(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddLocalization(configuration.Bind);
        }

        public static IServiceCollection AddLocalization(this IServiceCollection services, Action<LocalizationOptions> configureActions)
        {
            // need to localize OpenAPI: https://github.com/OAI/OpenAPI-Specification/issues/274
            // see: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-5.0

            services.AddRequestLocalization(o =>
            {
                o.ApplyCurrentCultureToResponseHeaders = true;
                o.DefaultRequestCulture = new RequestCulture("en"); 

                // FIXME: this should be driven from the localization store, and rebuilt when it changes
                var supportedCultures = new[] {"en", "fr"};
                o.SetDefaultCulture(supportedCultures[0]);
                o.AddSupportedCultures(supportedCultures);
                o.AddSupportedUICultures(supportedCultures);
                
                o.RequestCultureProviders.Clear();

                // ordered from explicit to implicit
                o.RequestCultureProviders.Add(new RouteDataRequestCultureProvider {Options = o});
                o.RequestCultureProviders.Add(new QueryStringRequestCultureProvider {Options = o});
                o.RequestCultureProviders.Add(new ClaimPrincipalCultureProvider {Options = o});
                o.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider {Options = o});
            });

            services.TryAddSingleton<ILocalizationStore, MemoryLocalizationStore>();
            services.TryAddSingleton<IStringLocalizerFactory, StringLocalizerFactory>();
            services.TryAddTransient(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));
            return services;
        }
    }
}
