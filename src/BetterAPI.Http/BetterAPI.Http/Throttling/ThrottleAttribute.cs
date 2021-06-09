// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using Microsoft.AspNetCore.Mvc;

namespace BetterAPI.Http.Throttling
{
    /// <summary>
    /// Throttles anonymous traffic to a controller action.
    /// </summary>
    public sealed class AllowAnonymousThrottleAttribute : ServiceFilterAttribute
    {
        public AllowAnonymousThrottleAttribute() : base(typeof(ThrottleFilter))
        {
            IsReusable = true;
        }
    }
}