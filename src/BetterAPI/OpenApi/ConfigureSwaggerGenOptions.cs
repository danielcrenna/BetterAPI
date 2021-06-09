// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BetterAPI.OpenApi
{
    public class ConfigureSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IStringLocalizer<ConfigureSwaggerGenOptions> _localizer;
        private readonly IOptionsMonitor<ApiOptions> _options;
        private readonly IApiVersionDescriptionProvider _provider;

        public ConfigureSwaggerGenOptions(IStringLocalizer<ConfigureSwaggerGenOptions> localizer, IOptionsMonitor<ApiOptions> options, IApiVersionDescriptionProvider provider)
        {
            _localizer = localizer;
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
                Title = _localizer.GetString(_options.CurrentValue.ApiName),
                Version = description.ApiVersion.ToString(),
                Description = _localizer.GetString(_options.CurrentValue.ApiDescription),
                Contact = new OpenApiContact { Name = _localizer.GetString(_options.CurrentValue.ApiContactName), Email = _localizer.GetString(_options.CurrentValue.ApiContactEmail) },
                License = new OpenApiLicense { Name = _localizer.GetString("Mozilla Public License 2.0"), Url = new Uri("https://opensource.org/licenses/MPL-2.0")}
            };

            if (description.IsDeprecated)
            {
                var deprecated = _localizer.GetString("[DEPRECATED]");
                info.Description += " " + deprecated;
            }

            return info;
        }
    }
}