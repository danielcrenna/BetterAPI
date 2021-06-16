// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Security.Claims;
using BetterAPI.ChangeLog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Demo
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddChangeLog(builder =>
            {
                builder.AddMissingResources();                  // allows omitting child resources from the version graph

                builder.AddResource<WeatherForecastV1>();
                builder.ShipVersionOne();
                
                builder.AddResource<WeatherForecastV2>();       // has child resource ReporterV1
                builder.ShipNextMinorVersion();
            });

            services.AddAuthorization(o =>
            {
                o.AddPolicy("TopSecret", builder =>
                {
                    builder.RequireAssertion(context => context.User.HasClaim(ClaimTypes.NameIdentifier, "Foo"));
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
        }
    }
}