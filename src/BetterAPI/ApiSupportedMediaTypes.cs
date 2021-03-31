// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace BetterAPI
{
    [Flags]
    public enum ApiSupportedMediaTypes
    {
        ApplicationJson
    }

    public static class ApiSupportedMediaTypesExtensions
    {
        public static bool HasFlagFast(this ApiSupportedMediaTypes value, ApiSupportedMediaTypes flag)
        {
            return (value & flag) != 0;
        }
    }
}