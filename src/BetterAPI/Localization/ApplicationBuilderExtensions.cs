// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using Microsoft.AspNetCore.Builder;

namespace BetterAPI.Localization
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseApiLocalization(this IApplicationBuilder app)
        {
            // see: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/localization-extensibility?view=aspnetcore-5.0
            app.UseRequestLocalization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDynamicControllerRoute<ApiRouter>("{culture}/{**route}");
            });
            return app;
        }
    }
}