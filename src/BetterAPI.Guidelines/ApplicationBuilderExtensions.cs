// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BetterAPI.Guidelines
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseApiGuidelines(this IApplicationBuilder app)
        {
            var options = app.ApplicationServices.GetService<IOptions<ApiOptions>>() ??
                          throw new InvalidOperationException("You must call AddApiGuidelines in ConfigureServices, before calling UseApiGuidelines in Configure");

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{options.Value.ApiName} {options.Value.ApiVersion}"));
            
            return app;
        }
    }
}