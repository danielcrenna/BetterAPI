// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using BetterAPI.Http.Interception;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace BetterAPI.Events
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseEventServices(this IApplicationBuilder app, IWebHostEnvironment environment)
        {
            if(environment.IsDevelopment())
                app.UseRequestInterception();

            return app;
        }
    }
}