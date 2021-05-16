// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.ComponentModel.DataAnnotations;
using BetterAPI.Caching;

namespace BetterAPI.Operations
{
    public sealed class Operation : IResource
    {
        [Required]
        public Guid Id { get; set; }

        public int Attempts { get; set; }
        public DateTimeOffset? LockedAt { get; set; }
        public string? LockedBy { get; set; }

        [LastModified]
        public DateTimeOffset CreatedAt { get; set; }
    }
}