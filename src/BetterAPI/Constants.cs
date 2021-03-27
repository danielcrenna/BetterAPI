// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using Microsoft.AspNetCore.Http;

namespace BetterAPI
{
    public static class Constants
    {
        /// <summary> The default section to pull from configuration when setting up the API. </summary>
        public const string DefaultConfigSection = nameof(BetterAPI);

        /// <summary>
        ///     The context key used to store the object result into `HttpContext.Items` when minimal representation is
        ///     preferred.
        /// </summary>
        public const string ObjectResultValue = nameof(ObjectResultValue);

        internal static readonly string CreatedStatusString = StatusCodes.Status201Created.ToString();

        public static class Prefer
        {
            public const string ReturnMinimal = "return=minimal";
            public const string ReturnRepresentation = "return=representation";
        }

        public static class Operators
        {
            public const string OrderBy = "$orderBy";
        }
    }
}