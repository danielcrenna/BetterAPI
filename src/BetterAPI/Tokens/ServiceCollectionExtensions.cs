using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

            services.AddSingleton<IEncryptionKeyStore, NoEncryptionKeyStore>();

            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,

                    ValidateAudience = true,
                    ValidAudience = options.Audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey))
                };
            });

            return services;
        }
    }
}
