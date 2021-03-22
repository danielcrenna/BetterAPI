// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BetterAPI.Guidelines.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace BetterAPI.Guidelines.Caching
{
    public sealed class HttpCacheFilterAttribute : IAsyncActionFilter
    {
        private readonly IHttpCache _cache;
        private readonly JsonSerializerOptions _options;

        public HttpCacheFilterAttribute(IHttpCache cache, JsonSerializerOptions options)
        {
            _cache = cache;
            _options = options;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.HttpContext.Request;
            var displayUrl = request.GetDisplayUrl();

            if (IsSafeRequest(request))
            {
                TryHandleSafeRequests(context, request, displayUrl);
            }
            else
            {
                TryHandleUnsafeRequests(context, request, displayUrl);
            }

            if (context.Result is StatusCodeResult)
                return;

            var executed = await next.Invoke();

            if (executed.Result is ObjectResult result)
            {
                var body = result.Value ?? executed.HttpContext.Items[ApiGuidelines.ObjectResultValue] ??
                    throw new NullReferenceException("Could not locate expected result body");

                GenerateAndAppendETag(context, body, displayUrl);

                GenerateAndAppendLastModified(context, body, displayUrl);

                if (IsSafeRequest(request))
                {
                    TryHandleSafeRequests(executed, request, displayUrl);
                }
                else
                {
                    TryHandleUnsafeRequests(executed, request, displayUrl);
                }
            }
        }
        
        private void TryHandleSafeRequests(ActionExecutingContext context, HttpRequest request, string displayUrl)
        {
            if (NoneMatchFailed(request, displayUrl) || NotModifiedSinceFailed(request, displayUrl) ||
                IfMatchFailed(request, displayUrl) || UnmodifiedSinceFailed(request, displayUrl))
            {
                context.Result = new StatusCodeResult((int) HttpStatusCode.NotModified);
            }
        }

        private void TryHandleUnsafeRequests(ActionExecutingContext context, HttpRequest request, string displayUrl)
        {
            if (IfMatchFailed(request, displayUrl) || UnmodifiedSinceFailed(request, displayUrl))
            {
                context.Result = new StatusCodeResult((int) HttpStatusCode.PreconditionFailed);
            }
        }

        private void TryHandleSafeRequests(ActionExecutedContext context, HttpRequest request, string displayUrl)
        {
            if (NoneMatchFailed(request, displayUrl) || NotModifiedSinceFailed(request, displayUrl))
            {
                context.Result = new StatusCodeResult((int) HttpStatusCode.NotModified);
            }
        }

        private void TryHandleUnsafeRequests(ActionExecutedContext context, HttpRequest request, string displayUrl)
        {
            if (NoneMatchFailed(request, displayUrl) || IfMatchFailed(request, displayUrl) ||
                NotModifiedSinceFailed(request, displayUrl) || UnmodifiedSinceFailed(request, displayUrl))
            {
                context.Result = PreconditionFailed(displayUrl);
            }
        }

        private static ObjectResult PreconditionFailed(string displayUrl)
        {
            return new ObjectResult(new ProblemDetails
            {
                Status = (int) HttpStatusCode.PreconditionFailed,
                Type = "https://httpstatuscodes.com/412",
                Title = "Precondition Failed",
                Detail = "The operation was aborted because it had unmet pre-conditions.",
                Instance = displayUrl
            });
        }

        private void GenerateAndAppendLastModified(ActionContext context, object body, string displayUrl)
        {
            var type = body.GetType();

            if (type.ImplementsGeneric(typeof(IEnumerable<>)) && body is IEnumerable enumerable)
            {
                DateTimeOffset? lastModified = default;
                foreach (var item in enumerable)
                    lastModified = GenerateAndAppendLastModified(context, item, type, displayUrl, lastModified);
            }
            else
            {
                GenerateAndAppendLastModified(context, body, type, displayUrl);
            }
        }

        private void GenerateAndAppendETag(ActionContext context, object body, string displayUrl)
        {
            var json = JsonSerializer.Serialize(body, _options);
            var etag = ETagGenerator.Generate(Encoding.UTF8.GetBytes(json));
            context.HttpContext.Response.Headers.Add(HeaderNames.ETag, new[] {etag.Value});
            _cache.Save(displayUrl, etag.Value);
        }

        private DateTimeOffset? GenerateAndAppendLastModified(ActionContext context, object body, Type type, string displayUrl, DateTimeOffset? lastModified = default)
        {
            var changed = false;
            var members = AccessorMembers.Create(type, AccessorMemberTypes.Properties, AccessorMemberScope.Public);
            foreach (var member in members)
                if (member.HasAttribute<LastModifiedAttribute>())
                {
                    var accessor = ReadAccessor.Create(body);
                    if (accessor.TryGetValue(body, member.Name, out var lastModifiedDate))
                    {
                        switch (lastModifiedDate)
                        {
                            case DateTimeOffset timestamp:
                                changed = true;
                                if (!lastModified.HasValue || lastModified < timestamp)
                                    lastModified = timestamp;
                                break;
                            case DateTime timestamp:
                                changed = true;
                                if(!lastModified.HasValue || lastModified < timestamp)
                                    lastModified = timestamp;
                                break;
                        }
                    }
                }

            if (!changed || !lastModified.HasValue)
                return lastModified;

            context.HttpContext.Response.Headers.Remove(HeaderNames.LastModified);
            context.HttpContext.Response.Headers.Add(HeaderNames.LastModified, lastModified.Value.ToString("R"));
            _cache.Save(displayUrl, lastModified.Value);
            return lastModified;
        }

        private bool NoneMatchFailed(HttpRequest request, string displayUrl) =>
            request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var ifNoneMatch) &&
            _cache.TryGetETag(BuildETagKey(displayUrl, ifNoneMatch), out var etag) && ifNoneMatch == etag;
        
        private bool IfMatchFailed(HttpRequest request, string displayUrl) =>
            request.Headers.TryGetValue(HeaderNames.IfMatch, out var ifMatch) &&
            _cache.TryGetETag(BuildETagKey(displayUrl, ifMatch), out var etag) && ifMatch != etag;

        private bool UnmodifiedSinceFailed(HttpRequest request, string displayUrl) =>
            request.Headers.TryGetValue(HeaderNames.IfUnmodifiedSince, out var ifUnmodifiedSince) &&
            DateTimeOffset.TryParse(ifUnmodifiedSince, out var ifUnmodifiedSinceDate) &&
            _cache.TryGetLastModified(BuildLastModifiedKey(displayUrl, ifUnmodifiedSince), out var lastModifiedDate) && lastModifiedDate > ifUnmodifiedSinceDate;

        private bool NotModifiedSinceFailed(HttpRequest request, string displayUrl) =>
            request.Headers.TryGetValue(HeaderNames.IfModifiedSince, out var ifModifiedSince) &&
            DateTimeOffset.TryParse(ifModifiedSince, out var ifModifiedSinceDate)
            && _cache.TryGetLastModified(BuildLastModifiedKey(displayUrl, ifModifiedSince), out var lastModifiedDate) &&
            lastModifiedDate <= ifModifiedSinceDate;
        
        private static string BuildETagKey(string displayUrl, StringValues values) => $"{displayUrl}_{HeaderNames.ETag}_{string.Join(",", (IEnumerable<string?>) values)}";
        private static string BuildLastModifiedKey(string displayUrl, StringValues values) => $"{displayUrl}_{HeaderNames.LastModified}_{string.Join(",", (IEnumerable<string?>) values)}";

        private static bool IsSafeRequest(HttpRequest request)
        {
            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/If-Match
            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/If-None-Match
            return request.Method == HttpMethods.Get || request.Method == HttpMethods.Head;
        }
    }
}