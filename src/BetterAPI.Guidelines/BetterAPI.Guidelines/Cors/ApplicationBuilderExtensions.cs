// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace BetterAPI.Guidelines.Cors
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseApiCors(this IApplicationBuilder app)
        {
            app.UseCors();
            return app;
        }

        public static IApplicationBuilder UseApiCors(this IApplicationBuilder app, string policyName)
        {
            app.UseCors(policyName);
            return app;
        }

        public static IApplicationBuilder UseApiCors(this IApplicationBuilder app, Action<CorsPolicyBuilder> configurePolicy)
        {
            app.UseCors(configurePolicy);
            return app;
        }
    }
}