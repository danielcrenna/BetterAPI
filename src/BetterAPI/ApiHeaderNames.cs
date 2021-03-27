// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using Microsoft.Net.Http.Headers;

namespace BetterAPI
{
    public static class ApiHeaderNames
    {
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public const string Prefer = "Prefer";
        public const string PreferenceApplied = "Preference-Applied";

        public static readonly string ETag = HeaderNames.ETag;
        public static readonly string LastModified = HeaderNames.LastModified;

        public static readonly string IfNoneMatch = HeaderNames.IfNoneMatch;
        public static readonly string IfMatch = HeaderNames.IfMatch;
        public static readonly string IfUnmodifiedSince = HeaderNames.IfUnmodifiedSince;
        public static readonly string IfModifiedSince = HeaderNames.IfModifiedSince;
    }
}