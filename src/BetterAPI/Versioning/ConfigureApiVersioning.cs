using System.Collections.Generic;
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

        public void Configure(ApiVersioningOptions options)
        {
            // Reporting api versions will return the headers "api-supported-versions" and "api-deprecated-versions"
            options.ReportApiVersions = true;
            options.ErrorResponses = new ProblemDetailsErrorResponseProvider();

            options.AssumeDefaultVersionWhenUnspecified = _options.CurrentValue.Versioning.AllowUnspecifiedVersions;

            ConfigureVersionReaders(options);

            // FIXME: default version could slide forward based on available versions by configuration?
            options.DefaultApiVersion = new ApiVersion(1, 0);
            
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
        }

        private void ConfigureVersionReaders(ApiVersioningOptions options)
        {
            var versionReaders = new HashSet<IApiVersionReader>();

            // 
            // Order matters:

            if (_options.CurrentValue.Versioning.UseClaims)
                versionReaders.Add(new ClaimsApiVersionReader());

            if (_options.CurrentValue.Versioning.UseUrl)
                versionReaders.Add(new UrlSegmentApiVersionReader());

            if (_options.CurrentValue.Versioning.UseQueryString)
                versionReaders.Add(new QueryStringApiVersionReader());

            if (_options.CurrentValue.Versioning.UseHeader)
                versionReaders.Add(new HeaderApiVersionReader());

            if (_options.CurrentValue.Versioning.UseMediaType)
                versionReaders.Add(new MediaTypeApiVersionReader());
            
            // FIXME: add a reader for claims-based version pinning

            if (versionReaders.Count > 0)
            {
                options.ApiVersionReader = ApiVersionReader.Combine(versionReaders);
            }
        }
    }
}
