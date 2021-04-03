using Microsoft.Extensions.DependencyInjection;

namespace BetterAPI.Versioning
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddVersioning(this IServiceCollection services)
        {
            services.AddApiVersioning(
                o =>
                {
                    // reporting api versions will return the headers "api-supported-versions" and "api-deprecated-versions"
                    o.ReportApiVersions = true;

                    //options.Conventions.Controller<ValuesController>().HasApiVersion( 1, 0 );

                    //options.Conventions.Controller<Values2Controller>()
                    //    .HasApiVersion( 2, 0 )
                    //    .HasApiVersion( 3, 0 )
                    //    .Action( c => c.GetV3( default ) ).MapToApiVersion( 3, 0 )
                    //    .Action( c => c.GetV3( default, default ) ).MapToApiVersion( 3, 0 );

                    //options.Conventions.Controller<HelloWorldController>()
                    //    .HasApiVersion( 1, 0 )
                    //    .HasApiVersion( 2, 0 )
                    //    .AdvertisesApiVersion( 3, 0 );
                } );
            services.AddVersionedApiExplorer(
                o =>
                {
                    // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                    // note: the specified format code will format the version as "'v'major[.minor][-status]"
                    o.GroupNameFormat = "'v'VVV";

                    // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
                    // can also be used to control the format of the API version in route templates
                    o.SubstituteApiVersionInUrl = true;
                });

            return services;
        }
    }
}
