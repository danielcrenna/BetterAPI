// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.ComponentModel.Design;

namespace BetterAPI
{
    public sealed class ResourceField
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Prompt { get; set; }
        public string? Type { get; set; }
        public string? PolicyName { get; set; }
        public ResourceVersion? Version { get; set; }
        public List<string> Options { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsHidden { get; set; }
        public int Order { get; set; }

        public ResourceField()
        {
            Options = new List<string>();
        }
    }
}