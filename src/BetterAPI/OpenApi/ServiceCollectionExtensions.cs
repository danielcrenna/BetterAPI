using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BetterAPI.OpenApi
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOpenApi(this IServiceCollection services, Assembly? assembly = default)
        {
            services.AddSingleton<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerGenOptions>();
            services.AddSwaggerGen(o =>
            {
                o.OperationFilter<VersioningOperationFilter>();
                o.DocumentFilter<DocumentationDocumentFilter>();
                o.OperationFilter<DocumentationOperationFilter>();
                o.SchemaFilter<DocumentationSchemaFilter>();

                // Set the comments path for the Swagger JSON and UI.
                // See: https://docs.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-5.0&tabs=visual-studio#xml-comments
                //
                var assemblyName = assembly?.GetName().Name;
                var xmlFile = $"{assemblyName}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if(File.Exists(xmlPath))
                    o.IncludeXmlComments(xmlPath, true);

                o.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
                {
                    Description = JwtBearerDefaults.AuthenticationScheme,
                    In  = ParameterLocation.Header,
                    Name = HeaderNames.Authorization,
                    Type = SecuritySchemeType.ApiKey
                });
            });
            return services;
        }
    }
}
