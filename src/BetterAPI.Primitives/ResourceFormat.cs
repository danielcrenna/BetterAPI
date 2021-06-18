// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;

namespace BetterAPI
{
    public sealed class ResourceFormat
    {
        public string? Name { get; set; }
        public ResourceVersion? Version { get; set; }
        public List<ResourceField> Fields { get; set; }
        public string? Type { get; set; }

        public ResourceFormat()
        {
            Fields = new List<ResourceField>();
        }
    }
}