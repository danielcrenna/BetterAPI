// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using BetterAPI.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace BetterAPI.Paging
{
    internal static class HttpResponseExtensions
    {
        public static void AppendLinkRelation(this HttpContext context, IDictionary<string, StringValues> queryInfo, string relation)
        {
            var queryUri = context.Request.GetDisplayUrlNoQueryString();
            var linkUri = QueryHelpers.AddQueryString(queryUri, queryInfo);
            context.Response.Headers.Append(ApiHeaderNames.Link, $"<{linkUri}>; rel=\"{relation}\"");
        }
    }
}