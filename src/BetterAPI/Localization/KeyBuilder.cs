// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text;
using BetterAPI.Data;
using WyHash;

namespace BetterAPI.Localization
{
    internal sealed class KeyBuilder
    {
        private static readonly byte[] LocalizationKeyPrefix = Encoding.UTF8.GetBytes("L:");
        private static readonly byte[] LocalizationCulturePrefix = Encoding.UTF8.GetBytes("C:");
        private static readonly byte[] LocalizationNamePrefix = Encoding.UTF8.GetBytes("N:");
        private static readonly byte[] LocalizationScopePrefix = Encoding.UTF8.GetBytes("S:");

        public static byte[] IndexOrLookupById(byte[] id) => LocalizationKeyPrefix.Concat(id);

        public static byte[] IndexByCultureName(string culture, byte[] id) => LocalizationCulturePrefix
            .Concat(LightningKeyBuilder.Key(culture))
            .Concat(IndexOrLookupById(id));

        public static byte[] IndexByName(string name, byte[] id) => LocalizationNamePrefix
            .Concat(LightningKeyBuilder.Key(name))
            .Concat(IndexOrLookupById(id));

        public static byte[] IndexByScope(string scope, byte[] id) => LocalizationScopePrefix
            .Concat(LightningKeyBuilder.Key(scope))
            .Concat(IndexOrLookupById(id));

        public static byte[] LookupByName(string name) => LocalizationNamePrefix
            .Concat(LightningKeyBuilder.Key(name));

        public static byte[] LookupByCultureName(string culture) => LocalizationCulturePrefix
            .Concat(LightningKeyBuilder.Key(culture));

    }
}