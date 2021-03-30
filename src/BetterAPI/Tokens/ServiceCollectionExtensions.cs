using System;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace BetterAPI.Tokens
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTokens(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddTokens(configuration.Bind);
        }

        public static IServiceCollection AddTokens(this IServiceCollection services, Action<TokenOptions>? configureAction = default)
        {
            TokenOptions options = new TokenOptions();

            if (configureAction != default)
            {
                services.AddOptions();
                services.Configure(configureAction);
                configureAction(options);
            }

            services.AddAuthentication()
                .AddJwtBearer(o =>
                {
                    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));

                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidAudience = options.Audience,
                        ValidIssuer = options.Issuer,
                        IssuerSigningKey = signingKey
                    };
                });

            return services;
        }
    }
}
