// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace BetterAPI.Serialization
{
    public static class ValueHash
    {
        private static readonly ulong DefaultSeed;

        static ValueHash()
        {
            DefaultSeed =
                BitConverter.ToUInt64(
                    new[]
                    {
                        (byte) 'd', (byte) 'e', (byte) 'a', (byte) 'd', (byte) 'b', (byte) 'e', (byte) 'e', (byte) 'f'
                    }, 0);
        }

        public static ulong? ComputeHash(object instance, IObjectSerializer objectSerializer = null,
            IStringSerializer stringSerializer = null, ITypeResolver typeResolver = null,
            IValueHashProvider valueHashProvider = null, ulong? seed = null)
        {
            stringSerializer ??= Defaults.StringSerializer;
            objectSerializer ??= Defaults.ObjectSerializer;
            typeResolver ??= Defaults.TypeResolver;

            return instance is string text
                ? ComputeHash(stringSerializer.ToBuffer(text, objectSerializer, typeResolver), valueHashProvider, seed)
                : ComputeHash(objectSerializer.ToBuffer(instance, typeResolver), valueHashProvider, seed);
        }

        public static ulong? ComputeHash(ReadOnlySpan<byte> buffer, IValueHashProvider valueHashProvider = null,
            ulong? seed = null)
        {
            return buffer == null
                ? default
                : (valueHashProvider ?? Defaults.ValueHashProvider).ComputeHash64(buffer, seed ?? DefaultSeed);
        }
    }
}