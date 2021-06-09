using System.Collections.Generic;
using BetterAPI.ChangeLog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Options;

namespace BetterAPI.Versioning
{
    internal sealed class ConfigureApiVersioning : IConfigureOptions<ApiVersioningOptions>
    {
        private readonly ChangeLogBuilder _builder;
        private readonly IOptionsMonitor<ApiOptions> _options;

        public ConfigureApiVersioning(ChangeLogBuilder builder, IOptionsMonitor<ApiOptions> options)
        {
            _builder = builder;
            _options = options;
        }

        public void Configure(ApiVersioningOptions options)
        {
            // Reporting api versions will return the headers "api-supported-versions" and "api-deprecated-versions"
            options.ReportApiVersions = true;
            options.ErrorResponses = new ProblemDetailsErrorResponseProvider();
            options.AssumeDefaultVersionWhenUnspecified = _options.CurrentValue.Versioning.AssumeDefaultVersionWhenUnspecified;

            ConfigureVersionReaders(options);

            // FIXME: default version could slide forward based on available versions by configuration?
            options.DefaultApiVersion = ApiVersion.Default; // new ApiVersion(1, 0)

            foreach (var (version, manifest) in _builder.Versions)
            {
                foreach (var item in manifest)
                {
                    var controllerType = typeof(ResourceController<>).MakeGenericType(item.Value);
                    var conventions = options.Conventions.Controller(controllerType);
                    conventions.HasApiVersion(version);
                }
            }
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
