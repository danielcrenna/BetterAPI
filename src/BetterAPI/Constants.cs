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
        public const string SinceContextKey = "since";
        public const string TopContextKey = "top";
        public const string ShapingContextKey = "shaping";
        public const string SearchContextKey = "search";
        public const string QueryContextKey = "query";

        public static class Prefer
        {
            public const string ReturnMinimal = "return=minimal";
            public const string ReturnRepresentation = "return=representation";
        }

        internal static readonly string Status200OkString = StatusCodes.Status200OK.ToString();
        internal static readonly string Status201CreatedString = StatusCodes.Status201Created.ToString();
        internal static readonly string Status204NoContentString = StatusCodes.Status204NoContent.ToString();
        internal static readonly string Status303SeeOtherString = StatusCodes.Status303SeeOther.ToString();
        internal static readonly string Status304NotModifiedString = StatusCodes.Status304NotModified.ToString();
        internal static readonly string Status400BadRequestString = StatusCodes.Status400BadRequest.ToString();
        internal static readonly string Status401UnauthorizedString = StatusCodes.Status401Unauthorized.ToString();
        internal static readonly string Status403ForbiddenString = StatusCodes.Status403Forbidden.ToString();
        internal static readonly string Status404NotFoundString = StatusCodes.Status404NotFound.ToString();
        internal static readonly string Status410GoneString = StatusCodes.Status410Gone.ToString();
        internal static readonly string Status412PreconditionFailedString = StatusCodes.Status412PreconditionFailed.ToString();
        internal static readonly string Status413PayloadTooLargeString = StatusCodes.Status413PayloadTooLarge.ToString();
        internal static readonly string Status500InternalServerErrorString = StatusCodes.Status500InternalServerError.ToString();

        internal static readonly string Get = nameof(ResourceController<IResource>.Get);
        internal static readonly string GetById = nameof(ResourceController<IResource>.GetById);
        internal static readonly string GetEmbedded = nameof(ResourceController<IResource>.GetEmbedded);
        internal static readonly string GetNextPage =  nameof(ResourceController<IResource>.GetNextPage);
        internal static readonly string Create = nameof(ResourceController<IResource>.Create);
        internal static readonly string Update = nameof(ResourceController<IResource>.Update);
        internal static readonly string Patch = nameof(ResourceController<IResource>.Patch);

        internal static readonly string Delete = nameof(Delete); // FIXME: there is no "DeleteWhere"
        internal static readonly string DeleteById = nameof(ResourceController<IResource>.DeleteById);
    }
}