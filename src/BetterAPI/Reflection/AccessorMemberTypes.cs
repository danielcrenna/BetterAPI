// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace BetterAPI.Reflection
{
    [Flags]
    public enum AccessorMemberTypes : byte
    {
        Fields = 1 << 1,
        Properties = 1 << 2,
        Methods = 1 << 3,

        None = 0x00,
        All = byte.MaxValue
    }
}