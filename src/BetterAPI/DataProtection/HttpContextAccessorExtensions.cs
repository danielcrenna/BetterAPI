// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace BetterAPI.DataProtection
{
    internal static class HttpContextAccessorExtensions
    {
        public static ClaimsPrincipal ResolveCurrentPrincipal(this IHttpContextAccessor? accessor)
        {
            return accessor?.HttpContext?.User ?? Thread.CurrentPrincipal as ClaimsPrincipal ??
                ClaimsPrincipal.Current ?? throw new NullReferenceException();
        }
    }
}