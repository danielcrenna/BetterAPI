// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using BetterAPI.Events;
using Microsoft.AspNetCore.Builder;

namespace BetterAPI.Http.Interception
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseRequestInterception(this IApplicationBuilder app)
        {
            app.UseMiddleware<RequestInterceptionMiddleware>();
            return app;
        }
    }
}