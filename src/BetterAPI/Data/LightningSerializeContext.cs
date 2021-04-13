// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using BetterAPI.Localization;
using BetterAPI.Reflection;
using LightningDB;

namespace BetterAPI.Data
{
    internal static class LightningSerializeContext
    {
        internal static readonly Dictionary<Type, (Func<object, byte[]> serialize, Func<MDBValue, object> deserialize)> KnownTypes;

        internal static readonly Dictionary<Type, Func<AccessorMember, ITypeReadAccessor, object, byte[], byte[]>> TryIndexMember;

        static LightningSerializeContext()
        {
            KnownTypes = new Dictionary<Type, (Func<object, byte[]> serialize, Func<MDBValue, object> deserialize)>
            {
                {typeof(LocalizationEntry), (SerializeLocalizationEntry, DeserializeLocalizationEntry)}
            };

            TryIndexMember = new Dictionary<Type, Func<AccessorMember, ITypeReadAccessor, object, byte[], byte[]>>
            {
                {typeof(LocalizationEntry), (member, accessor, target, id) =>
                {
                    if (!member.CanRead || !accessor.TryGetValue(target, member.Name, out var value))
                        return Array.Empty<byte>();

                    return member.Name switch
                    {
                        nameof(LocalizationEntry.Id) => LocalizationKeyBuilder.IndexOrLookupById(id),
                        nameof(LocalizationEntry.Key) => LocalizationKeyBuilder.IndexByKey((string) value, id),
                        nameof(LocalizationEntry.Culture) => LocalizationKeyBuilder.IndexByCultureName((string) value, id),
                        nameof(LocalizationEntry.Scope) => LocalizationKeyBuilder.IndexByScope((string) value, id),
                        _ => Array.Empty<byte>()
                    };
                }}
            };
        }

        private static byte[] SerializeLocalizationEntry(object value)
        {
            var entry = (LocalizationEntry) value;
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            var context = new LocalizationSerializationContext(bw);
            entry.Serialize(context);
            var buffer = ms.ToArray();
            return buffer;
        }

        private static unsafe object DeserializeLocalizationEntry(MDBValue value)
        {
            var buffer = value.AsSpan();

            fixed (byte* buf = &buffer.GetPinnableReference())
            {
                using var ms = new UnmanagedMemoryStream(buf, buffer.Length);
                using var br = new BinaryReader(ms);
                var context = new LocalizationDeserializationContext(br);
                var entry = new LocalizationEntry(context);
                return entry;
            }
        }
    }
}