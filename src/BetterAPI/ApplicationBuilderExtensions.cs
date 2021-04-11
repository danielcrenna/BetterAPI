// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using BetterAPI.Localization;
using BetterAPI.Metrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace BetterAPI
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseApiServer(this IApplicationBuilder app)
        {
            app.UseServerTiming();

            app.Map("/ping", HandlePing);

            app.Use(async (context, next) =>
            {
                // FIXME: this is likely only needed with the test server
                AppendDateHeaderIfNotPresent(app, context);

                app.AddServerHeader(context);

                await next.Invoke();
            });
            
            app.UseHttpsRedirection();
            app.UseApiGuidelines();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseApiLocalization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDynamicControllerRoute<ApiRouter>("{**route}");
            });

            return app;
        }

        public static IApplicationBuilder UseApiGuidelines(this IApplicationBuilder app)
        {
            var options = app.ApplicationServices.GetService<IOptions<ApiOptions>>() ??
                          throw new InvalidOperationException(
                              "You must call AddApiGuidelines in ConfigureServices, before calling UseApiGuidelines in Configure");

            app.UseCors();
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

        private static void HandlePing(IApplicationBuilder app)
        {
            app.Run(context =>
            {
                context.Response.StatusCode = (int) HttpStatusCode.OK;

                // see: https://tools.ietf.org/html/rfc7231#section-7.1.1.2
                AppendDateHeaderIfNotPresent(app, context);

                // see: https://tools.ietf.org/html/rfc7230#section-3.3.2
                context.Response.ContentLength = 0;

                app.AddServerHeader(context);

                // being explicit about what we're going to do after the response is sent
                context.Response.Headers.TryAdd(HeaderNames.Connection, "close");

                return Task.CompletedTask;
            });
        }

        private static void AppendDateHeaderIfNotPresent(IApplicationBuilder app, HttpContext context)
        {
            // See: https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#75-standard-request-headers
            // Microsoft REST Guidelines points to https://tools.ietf.org/html/rfc5322#section-3.3
            // But RFC 1123 seems to point back to RFC 5322 or is otherwise identical.
            //
            // Also guidelines specifies GMT (ala RFC 1123), and ignores the stipulations of RFC 5322:
            // 'The date and time-of-day SHOULD express local time.'

            if (!context.Response.HasStarted && !context.Response.Headers.ContainsKey(HeaderNames.Date))
            {
                // Always send RFC 1123 date format:
                // See: https://www.w3.org/Protocols/rfc2616/rfc2616-sec3.html#sec3.3

                var timestamps = app.ApplicationServices.GetRequiredService<Func<DateTimeOffset>>();
                context.Response.Headers.TryAdd(HeaderNames.Date, timestamps().ToString("R"));
            }
        }

        private static void AddServerHeader(this IApplicationBuilder app, HttpContext context)
        {
            // see: https://tools.ietf.org/html/rfc7231#section-7.4.2
            context.Response.Headers.TryAdd(HeaderNames.Server, app.GetServerHeaderValue());
        }

        private static string GetServerHeaderValue(this IApplicationBuilder app)
        {
            var options = app.ApplicationServices.GetRequiredService<IOptions<ApiOptions>>();
            return options.Value.ApiServer;
        }
    }
}