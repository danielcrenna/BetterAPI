// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Text;
using BetterAPI.Data;

namespace BetterAPI.Localization
{
    internal sealed class KeyBuilder
    {
        private static readonly byte[] LocalizationKeyPrefix = Encoding.UTF8.GetBytes("L:");
        private static readonly byte[] LocalizationCulturePrefix = Encoding.UTF8.GetBytes("C:");
        private static readonly byte[] LocalizationNamePrefix = Encoding.UTF8.GetBytes("N:");

        public static byte[] LookupById(byte[] id) => LocalizationKeyPrefix.Concat(id);

        public static byte[] LookupByCultureName(string culture) => LocalizationCulturePrefix
            .Concat(Encoding.UTF8.GetBytes(culture));

        public static byte[] IndexByCultureName(string culture, byte[] id) => LocalizationCulturePrefix
            .Concat(Encoding.UTF8.GetBytes(culture))
            .Concat(LookupById(id));

        public static byte[] LookupByName(string name) => LocalizationNamePrefix
            .Concat(Encoding.UTF8.GetBytes(name));

        public static byte[] IndexByName(string name, byte[] id) => LocalizationNamePrefix
            .Concat(Encoding.UTF8.GetBytes(name))
            .Concat(LookupById(id));

        public static byte[] GetAllEntriesKey() => LocalizationKeyPrefix;
    }
}