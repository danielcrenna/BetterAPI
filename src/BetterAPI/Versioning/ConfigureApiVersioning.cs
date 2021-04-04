using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Options;

namespace BetterAPI.Versioning
{
    internal sealed class ConfigureApiVersioning : IConfigureOptions<ApiVersioningOptions>
    {
        private readonly IOptionsMonitor<ApiOptions> _options;

        public ConfigureApiVersioning(IOptionsMonitor<ApiOptions> options)
        {
            _options = options;
        }

        public void Configure(ApiVersioningOptions o)
        {
            // reporting api versions will return the headers "api-supported-versions" and "api-deprecated-versions"
            o.ReportApiVersions = true;
            o.ErrorResponses = new ProblemDetailsErrorResponseProvider();
            o.AssumeDefaultVersionWhenUnspecified = _options.CurrentValue.Versioning.AllowUnspecifiedVersions;

            // FIXME: default version could slide forward based on available versions by configuration?
            o.DefaultApiVersion = new ApiVersion(1, 0);
            
            //o.Conventions.Controller<ValuesController>().HasApiVersion( 1, 0 );

            //o.Conventions.Controller<Values2Controller>()
            //    .HasApiVersion( 2, 0 )
            //    .HasApiVersion( 3, 0 )
            //    .Action( c => c.GetV3( default ) ).MapToApiVersion( 3, 0 )
            //    .Action( c => c.GetV3( default, default ) ).MapToApiVersion( 3, 0 );

            //o.Conventions.Controller<HelloWorldController>()
            //    .HasApiVersion( 1, 0 )
            //    .HasApiVersion( 2, 0 )
            //    .AdvertisesApiVersion( 3, 0 );
        }
    }
}
