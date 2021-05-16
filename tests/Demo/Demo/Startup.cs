// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Security.Claims;
using BetterAPI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
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
                builder.AddResource<WeatherForecastV1>("WeatherForecast");
                builder.ShipVersion(ApiVersion.Default);

                // FIXME: auto-add or throw if we don't add the dependent Reporter resource
                builder.AddResource<WeatherForecastV2>("WeatherForecast");
                builder.AddResource<ReporterV1>("Reporter"); 
                builder.ShipVersion(new ApiVersion(1, 1));
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