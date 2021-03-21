// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using Microsoft.AspNetCore.Builder;

namespace BetterApi.Guidelines
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseApiGuidelines(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BetterAPI v1"));
            return app;
        }
    }
}