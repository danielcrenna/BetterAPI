// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using BetterAPI.Data;

namespace BetterAPI.Localization
{
    public sealed class LocalizationDeserializationContext
    {
        // ReSharper disable once InconsistentNaming
        public readonly BinaryReader br;

        public LocalizationDeserializationContext(BinaryReader br)
        {
            this.br = br;
            Version = br.ReadUInt64();
            if (Version > LocalizationSerializationContext.FormatVersion)
                throw new Exception("Tried to load localization entry with a version that is too new");
        }

        public ulong Version { get; }
    }
}