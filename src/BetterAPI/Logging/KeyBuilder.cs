// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Text;
using Microsoft.Extensions.Logging;

namespace BetterAPI.Logging
{
    internal static class KeyBuilder
    {
        public static byte[] BuildLogLevelKey(LoggingEntry entry)
        {
            return Encoding.UTF8.GetBytes($"{GetAllLogsByLevelKey(entry.LogLevel)}:{entry.Id}");
        }

        public static byte[] GetAllLogsByLevelKey(LogLevel level)
        {
            return Encoding.UTF8.GetBytes($"L:{level}");
        }

        public static byte[] BuildLogByIdKey(LoggingEntry entry)
        {
            return Encoding.UTF8.GetBytes($"E:{entry.Id}");
        }

        public static byte[] GetAllLogsKey()
        {
            return Encoding.UTF8.GetBytes("E:");
        }
    }
}