// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using BetterAPI.Reflection;
using Microsoft.AspNetCore.Http;

namespace BetterAPI.Extensions
{
    internal static class HttpRequestExtensions
    {
        private const string SchemeDelimiter = "://";

        public static string GetDisplayUrlNoQueryString(this HttpRequest request)
        {
            return Pooling.StringBuilderPool.Scoped(sb =>
            {
                sb.Append(request.Scheme)
                    .Append(SchemeDelimiter)
                    .Append(request.Host.Value)
                    .Append(request.PathBase.Value)
                    .Append(request.Path.Value);
            });
        }
    }
}