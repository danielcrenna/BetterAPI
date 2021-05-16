// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Text;
using BetterAPI.Data;
using BetterAPI.Data.Lmdb;

namespace BetterAPI.Localization
{
    public sealed class LocalizationKeyBuilder
    {
        public static readonly byte[] LocalizationCulturePrefix = Encoding.UTF8.GetBytes("L:C:");
        public static readonly byte[] LocalizationKeyPrefix = Encoding.UTF8.GetBytes("L:N:");
        public static readonly byte[] LocalizationScopePrefix = Encoding.UTF8.GetBytes("L:S:");

        public static byte[] LookupByKey(string name) => LocalizationKeyPrefix
            .Concat(KeyBuilder.PrepareValue(name));

        public static byte[] LookupByCulture(string culture) => LocalizationCulturePrefix
            .Concat(KeyBuilder.PrepareValue(culture));

        public static byte[] LookupByScope(string scope) => LocalizationScopePrefix
            .Concat(KeyBuilder.PrepareValue(scope));

    }
}