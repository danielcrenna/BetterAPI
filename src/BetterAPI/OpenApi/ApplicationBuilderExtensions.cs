// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BetterAPI.OpenApi
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseOpenApi(this IApplicationBuilder app, IOptions<ApiOptions> options)
        {
            app.UseSwagger(o => { o.RouteTemplate = options.Value.OpenApiSpecRouteTemplate; });
            app.UseSwaggerUI(c =>
            {
                c.EnableDeepLinking();
                c.RoutePrefix = options.Value.OpenApiUiRoutePrefix?.TrimStart('/');

                var provider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    var url = options.Value.OpenApiSpecRouteTemplate?.Replace("{documentname}", description.GroupName);
                    if (url != null && !url.StartsWith('/'))
                        url = url.Insert(0, "/");

                    c.SwaggerEndpoint(url, $"{options.Value.ApiName} {description.GroupName}");
                }
            });
            return app;
        }
    }
}