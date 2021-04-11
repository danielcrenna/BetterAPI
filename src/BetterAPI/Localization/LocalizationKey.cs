// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using BetterAPI.Data;

namespace BetterAPI.Localization
{
    public readonly struct LocalizationEntry
    {
        public Guid Id { get; }
        public string Culture { get; }
        public string Key { get; }
        public string? Value { get; }
        public bool IsMissing { get; }

        public LocalizationEntry(Guid id, string culture, string key, string? value, bool missing)
        {
            Id = id;
            Culture = culture;
            Key = key;
            Value = value;
            IsMissing = missing;
        }

        public LocalizationEntry(LocalizationDeserializationContext context)
        {
            Id = context.br.ReadGuid();
            Culture = context.br.ReadString();
            Key = context.br.ReadString();
            Value = context.br.ReadNullableString();
            IsMissing = context.br.ReadBoolean();
        }

        public void Serialize(LocalizationSerializationContext context)
        {
            context.bw.Write(Id);
            context.bw.Write(Culture);
            context.bw.Write(Key);
            context.bw.WriteNullableString(Value);
            context.bw.Write(IsMissing);
        }
    }
}