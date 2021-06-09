// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

namespace BetterAPI.Guidelines.Cors
{
    public sealed class CorsOptions
    {
        /// <summary>
        /// Whether to echo back the request's origin, rather than provide "*". 
        /// The default is false.
        ///     <remarks>
        ///         This is of marginal value when APIs are flagged by static analysis tools for including "*" as the origin,
        ///         even when the Microsoft REST API Guidelines state that the service SHOULD support all origins.
        ///
        ///         This mimics implementing a origin-specific CORS policy, but does not require exposing any domains to the
        ///         requester.
        ///     </remarks>
        /// </summary>
        public bool EchoOrigin { get; set; } = false;
    }
}
