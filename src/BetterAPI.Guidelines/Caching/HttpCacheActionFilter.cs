// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

namespace BetterAPI.Guidelines.Caching
{
    public sealed class HttpCacheActionFilter : IAsyncActionFilter
    {
        private readonly IHttpCache _cache;
        private readonly JsonSerializerOptions _options;

        public HttpCacheActionFilter(IHttpCache cache, JsonSerializerOptions options)
        {
            _cache = cache;
            _options = options;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.ActionDescriptor.EndpointMetadata.Any(x => x is DoNotHttpCacheAttribute))
            {
                await next.Invoke();
                return;
            }

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
                var body = result.Value ?? executed.HttpContext.Items[Constants.ObjectResultValue] ??
                    throw new NullReferenceException("Could not locate expected result body");

                GenerateAndAppendETag(context, body, displayUrl);
                GenerateAndAppendLastModified(context, body, displayUrl);

                // we have to check again in case the resulting operation refreshed the cache
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
            // Before execution, if the cache misses, we have to let the request execute, in case the cache
            // would otherwise populate with the value for this check during execution

            if (IfNoneMatchFailed(request, displayUrl, true)
                || IfMatchFailed(request, displayUrl, true)
                //|| NotModifiedSinceFailed(request, displayUrl)
                //|| UnmodifiedSinceFailed(request, displayUrl)
                )
            {
                context.Result = new StatusCodeResult((int) HttpStatusCode.NotModified);
            }
        }

        private void TryHandleUnsafeRequests(ActionExecutingContext context, HttpRequest request, string displayUrl)
        {
            // After execution, we can assume that a cache miss is legitimate, since the server
            // has had the opportunity to populate the cache with the requested value prior to this check

            if (IfNoneMatchFailed(request, displayUrl, true) 
                || IfMatchFailed(request, displayUrl, true) 
                //|| NotModifiedSinceFailed(request, displayUrl) 
                //|| UnmodifiedSinceFailed(request, displayUrl)
                )
            {
                context.Result = PreconditionFailed(displayUrl);
            }
        }

        private void TryHandleSafeRequests(ActionExecutedContext context, HttpRequest request, string displayUrl)
        {
            if (IfNoneMatchFailed(request, displayUrl, false) 
                || IfMatchFailed(request, displayUrl, false) 
                //|| NotModifiedSinceFailed(request, displayUrl) 
                //|| UnmodifiedSinceFailed(request, displayUrl)
                )
            {
                context.Result = new StatusCodeResult((int) HttpStatusCode.NotModified);
            }
        }

        private void TryHandleUnsafeRequests(ActionExecutedContext context, HttpRequest request, string displayUrl)
        {
            if (IfNoneMatchFailed(request, displayUrl, false) 
                || IfMatchFailed(request, displayUrl, false) 
                //|| NotModifiedSinceFailed(request, displayUrl) 
                //|| UnmodifiedSinceFailed(request, displayUrl)
                )
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
            context.HttpContext.Response.Headers.Add(ApiHeaderNames.ETag, new[] {etag.Value});
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

            context.HttpContext.Response.Headers.Remove(ApiHeaderNames.LastModified);
            context.HttpContext.Response.Headers.Add(ApiHeaderNames.LastModified, lastModified.Value.ToString("R"));
            _cache.Save(displayUrl, lastModified.Value);
            return lastModified;
        }

        /// <summary>  The If-None-Match header is present and we have a match for the provided ETag </summary>
        private bool IfNoneMatchFailed(HttpRequest request, string displayUrl, bool skipCheckOnCacheMiss)
        {
            if (!request.Headers.TryGetValue(ApiHeaderNames.IfNoneMatch, out var values))
                return false; // no comparison needed
            
            if(!_cache.TryGetETag(BuildETagKey(displayUrl, values), out var etag) && skipCheckOnCacheMiss)
                return !skipCheckOnCacheMiss; // cache miss, so none match (or skip this check)

            // if they match, we fail
            return values == etag;
        }
        
        /// <summary>  The If-Match header is present and we don't have a match for the provided ETag </summary>
        private bool IfMatchFailed(HttpRequest request, string displayUrl, bool skipCheckOnCacheMiss)
        {
            if (!request.Headers.TryGetValue(ApiHeaderNames.IfMatch, out var values))
                return false; // no comparison needed

            if(!_cache.TryGetETag(BuildETagKey(displayUrl, values), out var etag) && skipCheckOnCacheMiss)
                return !skipCheckOnCacheMiss; // cache miss, so none match (or skip this check)

            // if they don't match, we fail
            return values != etag;
        }

        private bool UnmodifiedSinceFailed(HttpRequest request, string displayUrl) =>
            request.Headers.TryGetValue(ApiHeaderNames.IfUnmodifiedSince, out var ifUnmodifiedSince) &&
            DateTimeOffset.TryParse(ifUnmodifiedSince, out var ifUnmodifiedSinceDate) &&
            _cache.TryGetLastModified(BuildLastModifiedKey(displayUrl, ifUnmodifiedSince), out var lastModifiedDate) && lastModifiedDate > ifUnmodifiedSinceDate;

        private bool NotModifiedSinceFailed(HttpRequest request, string displayUrl) =>
            request.Headers.TryGetValue(ApiHeaderNames.IfModifiedSince, out var ifModifiedSince) &&
            DateTimeOffset.TryParse(ifModifiedSince, out var ifModifiedSinceDate)
            && _cache.TryGetLastModified(BuildLastModifiedKey(displayUrl, ifModifiedSince), out var lastModifiedDate) &&
            lastModifiedDate <= ifModifiedSinceDate;
        
        private static string BuildETagKey(string displayUrl, StringValues values) => $"{displayUrl}_{ApiHeaderNames.ETag}_{string.Join(",", (IEnumerable<string?>) values)}";
        private static string BuildLastModifiedKey(string displayUrl, StringValues values) => $"{displayUrl}_{ApiHeaderNames.LastModified}_{string.Join(",", (IEnumerable<string?>) values)}";

        private static bool IsSafeRequest(HttpRequest request)
        {
            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/If-Match
            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/If-None-Match
            return request.Method == HttpMethods.Get || request.Method == HttpMethods.Head;
        }
    }
}