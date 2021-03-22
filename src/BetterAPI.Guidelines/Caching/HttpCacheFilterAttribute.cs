// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BetterApi.Guidelines.Caching;
using BetterAPI.Guidelines.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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
            if (request.Method == HttpMethods.Get)
            {
                TryHandleSafeRequests(context, request);
            }
            else
            {
                TryHandleUnsafeRequests(context, request);
            }

            if (context.Result is StatusCodeResult)
                return;

            var executed = await next.Invoke();

            if (executed.Result is ObjectResult result)
            {
                MaybeAppendCacheValues(context, result.Value);

                if (request.Method == HttpMethods.Get)
                {
                    TryHandleSafeRequests(context, request);
                }
                else
                {
                    TryHandleUnsafeRequests(context, request);
                }
            }
        }

        private void TryHandleSafeRequests(ActionExecutingContext context, HttpRequest request)
        {
            var key = request.GetDisplayUrl();

            if (NoneMatch(request, key) || NotModifiedSince(request, key))
            {
                context.Result = new StatusCodeResult((int) HttpStatusCode.NotModified);
            }
        }

        private void TryHandleUnsafeRequests(ActionExecutingContext context, HttpRequest request)
        {
            var key = request.GetDisplayUrl();

            if (IfMatchFailed(request, key) || UnmodifiedSinceFailed(request, key))
            {
                context.Result = new StatusCodeResult((int) HttpStatusCode.PreconditionFailed);
            }
        }

        private void MaybeAppendCacheValues(ActionContext context, object body)
        {
            var cacheKey = context.HttpContext.Request.GetDisplayUrl();
            var json = JsonSerializer.Serialize(body, _options);
            var etag = ETagGenerator.Generate(Encoding.UTF8.GetBytes(json));
            context.HttpContext.Response.Headers.Add(HeaderNames.ETag, new[] {etag.Value});
            _cache.Save(cacheKey, etag.Value);

            var members = AccessorMembers.Create(body, AccessorMemberTypes.Properties, AccessorMemberScope.Public);
            foreach (var member in members)
                if (member.HasAttribute<CacheTimestampAttribute>())
                {
                    var accessor = ReadAccessor.Create(body);
                    if (accessor.TryGetValue(body, member.Name, out var lastModifiedDate))
                    {
                        switch (lastModifiedDate)
                        {
                            case DateTimeOffset timestamp:
                                context.HttpContext.Response.Headers.Add(HeaderNames.LastModified,
                                    timestamp.ToString("R"));
                                _cache.Save(cacheKey, timestamp);
                                break;
                            case DateTime timestamp:
                                context.HttpContext.Response.Headers.Add(HeaderNames.LastModified,
                                    timestamp.ToString("R"));
                                _cache.Save(cacheKey, timestamp);
                                break;
                        }
                    }
                }
        }

        private bool NoneMatch(HttpRequest request, string key) =>
            request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var ifNoneMatch) &&
            _cache.TryGetETag(key, out var etag) && ifNoneMatch == etag;

        private bool IfMatchFailed(HttpRequest request, string key) =>
            request.Headers.TryGetValue(HeaderNames.IfMatch, out var ifMatch) &&
            _cache.TryGetETag(key, out var etag) && ifMatch != etag;

        private bool UnmodifiedSinceFailed(HttpRequest request, string key) =>
            request.Headers.TryGetValue(HeaderNames.IfUnmodifiedSince, out var ifUnmodifiedSince) &&
            DateTimeOffset.TryParse(ifUnmodifiedSince, out var ifUnmodifiedSinceDate) &&
            _cache.TryGetLastModified(key, out var lastModifiedDate) && lastModifiedDate > ifUnmodifiedSinceDate;

        private bool NotModifiedSince(HttpRequest request, string key) =>
            request.Headers.TryGetValue(HeaderNames.IfModifiedSince, out var ifModifiedSince) &&
            DateTimeOffset.TryParse(ifModifiedSince, out var ifModifiedSinceDate)
            && _cache.TryGetLastModified(key, out var lastModifiedDate) &&
            lastModifiedDate <= ifModifiedSinceDate;
    }
}