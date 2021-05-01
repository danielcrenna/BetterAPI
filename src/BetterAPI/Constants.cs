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
        ///     The context key used to store the object result into `HttpContext.Items` when no other middleware is permitted
        ///     to modify the result (i.e. when minimal representation is preferred)
        /// </summary>
        public const string TerminalObjectResultValue = nameof(TerminalObjectResultValue);

        public const string SortContextKey = "sort";
        public const string FilterOperationContextKey = "filter";
        public const string MaxPageSizeContextKey = "maxpagesize";
        public const string CountContextKey = "count";
        public const string CountResultContextKey = "countresult";
        public const string SkipContextKey = "skip";
        public const string TopContextKey = "top";
        public const string IncludeContextKey = "include";
        public const string QueryContextKey = "query";

        public static class Prefer
        {
            public const string ReturnMinimal = "return=minimal";
            public const string ReturnRepresentation = "return=representation";
        }

        internal static readonly string Status201CreatedString = StatusCodes.Status201Created.ToString();

        internal static readonly string Get = nameof(Get);
        internal static readonly string GetById = nameof(GetById);
        internal static readonly string GetNextPage = nameof(GetNextPage);
        internal static readonly string Create = nameof(Create);
        internal static readonly string Update = nameof(Update);
        internal static readonly string Delete = nameof(Delete);
        internal static readonly string DeleteById = nameof(DeleteById);
    }
}