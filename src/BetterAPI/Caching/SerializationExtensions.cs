// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;

namespace BetterAPI.Caching
{
    public static class SerializationExtensions
    {
        #region Booleans

        public static bool ReadBoolean(this BinaryReader br)
        {
            return br.ReadBoolean();
        }

        public static bool WriteBoolean(this BinaryWriter bw, bool value)
        {
            bw.Write(value);
            return value;
        }

        #endregion

        #region TimeSpan

        public static void WriteTimeSpan(this BinaryWriter bw, TimeSpan value)
        {
            bw.Write(value.Ticks);
        }

        public static TimeSpan ReadTimeSpan(this BinaryReader br)
        {
            return TimeSpan.FromTicks(br.ReadInt64());
        }

        #endregion

        #region DateTimeOffset

        public static void WriteDateTimeOffset(this BinaryWriter bw, DateTimeOffset value)
        {
            bw.WriteTimeSpan(value.Offset);
            bw.Write(value.Ticks);
        }

        public static object ReadDateTimeOffset(this BinaryReader br)
        {
            var offset = br.ReadTimeSpan();
            var ticks = br.ReadInt64();
            return new DateTimeOffset(new DateTime(ticks), offset);
        }

        #endregion
    }
}