// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text;

namespace BetterAPI.Logging
{
    internal static class KeyBuilder
    {
        private static readonly byte[] LogEntryPrefix = Encoding.UTF8.GetBytes("E:");
        private static readonly byte[] LogDataKeyPrefix = Encoding.UTF8.GetBytes("K:");
        private static readonly byte[] LogDataValuePrefix = Encoding.UTF8.GetBytes(":V:");

        public static byte[] BuildLogByEntryKey(byte[] id) => LogEntryPrefix.Concat(id);
        public static byte[] BuildLogByDataKey(string key) => LogDataKeyPrefix.Concat(Encoding.UTF8.GetBytes(key)).Concat(LogDataValuePrefix);

        public static byte[] BuildLogByDataKey(string key, string? value) => LogDataKeyPrefix
            .Concat(Encoding.UTF8.GetBytes(key)).Concat(LogDataValuePrefix)
            .Concat(Encoding.UTF8.GetBytes(value ?? string.Empty));

        public static byte[] GetAllLogEntriesKey() => LogEntryPrefix;

        private static byte[] Concat(this byte[] left, byte[] right)
        {
            var buffer = new byte[left.Length + right.Length];
            Buffer.BlockCopy(left, 0, buffer, 0, left.Length);
            Buffer.BlockCopy(right, 0, buffer, left.Length, right.Length);
            return buffer;
        }
    }
}