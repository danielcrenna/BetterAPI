// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;

namespace BetterAPI.Logging
{
    public sealed class LoggingSerializeContext
    {
        public const ulong FormatVersion = 1UL;

        // ReSharper disable once InconsistentNaming
        public readonly BinaryWriter bw;

        public LoggingSerializeContext(BinaryWriter bw, ulong version = FormatVersion)
        {
            this.bw = bw;
            if (Version > FormatVersion)
                throw new Exception("Tried to save log entry with a version that is too new");
            Version = version;
            bw.Write(Version);
        }

        public ulong Version { get; }
    }
}