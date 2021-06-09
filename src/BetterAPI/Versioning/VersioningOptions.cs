// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Security.Permissions;

namespace BetterAPI.Versioning
{
    public sealed class VersioningOptions
    {
        public bool AssumeDefaultVersionWhenUnspecified { get; set; } = true;
        public bool UseUrl { get; set; } = true;
        public bool UseQueryString { get; set; } = true;
        public bool UseHeader { get; set; } = true;
        public bool UseMediaType { get; set; } = true;
        public bool UseClaims { get; set; } = true;
    }
}