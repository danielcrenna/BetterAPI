// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using BetterAPI.Data;
using Microsoft.Extensions.Localization;

namespace BetterAPI.Localization
{
    public readonly struct LocalizationEntry
    {
        [Index]
        public Guid Id { get; }

        [Index]
        public string Culture { get; }

        [Index]
        public string Scope { get; }

        [Index]
        public string Key { get; }

        public string? Value { get; }

        [Index]
        public bool IsMissing { get; }

        public LocalizedString AsLocalizedString => new LocalizedString(Key, Value ?? Key, IsMissing, Scope);

        public LocalizationEntry(Guid id, string culture, LocalizedString value)
        {
            Id = id;
            Culture = culture;
            Scope = value.SearchedLocation ?? string.Empty;
            Key = value.Name;
            Value = value.Value;
            IsMissing = value.ResourceNotFound;
        }

        public LocalizationEntry(LocalizationDeserializationContext context)
        {
            Id = context.br.ReadGuid();
            Culture = context.br.ReadString();
            Scope = context.br.ReadString();
            Key = context.br.ReadString();
            Value = context.br.ReadNullableString();
            IsMissing = context.br.ReadBoolean();
        }

        public void Serialize(LocalizationSerializationContext context)
        {
            context.bw.Write(Id);
            context.bw.Write(Culture);
            context.bw.Write(Scope);
            context.bw.Write(Key);
            context.bw.WriteNullableString(Value);
            context.bw.Write(IsMissing);
        }
    }
}