// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BetterAPI
{
    public class ConfigureSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IOptionsMonitor<ApiOptions> _options;
        readonly IApiVersionDescriptionProvider _provider;

        public ConfigureSwaggerGenOptions(IOptionsMonitor<ApiOptions> options, IApiVersionDescriptionProvider provider)
        {
            _options = options;
            _provider = provider;
        }

        public void Configure(SwaggerGenOptions options)
        {
            foreach (var description in _provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
            }
        }

        private OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
        {
            var info = new OpenApiInfo
            {
                Title = _options.CurrentValue.ApiName,
                Version = description.ApiVersion.ToString(),
                Description = _options.CurrentValue.ApiDescription,
                Contact = new OpenApiContact { Name = _options.CurrentValue.ApiContactName, Email =_options.CurrentValue.ApiContactEmail },
                License = new OpenApiLicense { Name = "Mozilla Public License 2.0", Url = new Uri("https://opensource.org/licenses/MPL-2.0")}
            };

            if (description.IsDeprecated)
            {
                info.Description += " This API version has been deprecated.";
            }

            return info;
        }
    }
}