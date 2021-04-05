// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace BetterAPI.Logging
{
    internal static class KeyBuilder
    {
        private static readonly byte[] LogLevelPrefix = Encoding.UTF8.GetBytes("L:");
        private static readonly byte[] LogEntryPrefix = Encoding.UTF8.GetBytes("E:");

        public static byte[] BuildLogByLevelKey(LogLevel level) => LogLevelPrefix.Concat(Encoding.UTF8.GetBytes(level.ToString()));
        public static byte[] BuildLogByIdKey(LoggingEntry entry) => LogEntryPrefix.Concat(entry.Id.ToByteArray());
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