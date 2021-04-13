// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace BetterAPI.Data
{
    internal static class BufferExtensions
    {
        #region Boolean

        public static bool WriteBoolean(this BinaryWriter bw, bool value)
        {
            bw.Write(value);
            return value;
        }

        #endregion

        #region Nullable<String>

        public static void WriteNullableString(this BinaryWriter bw, string? value)
        {
            if (bw.WriteBoolean(value != null))
                bw.Write(value!);
        }

        public static string? ReadNullableString(this BinaryReader br)
        {
            return br.ReadBoolean() ? br.ReadString() : null;
        }

        #endregion

        #region Nullable<UInt64>

        public static void WriteNullableUInt64(this BinaryWriter bw, ulong? value)
        {
            if (bw.WriteBoolean(value.HasValue))
                // ReSharper disable once PossibleInvalidOperationException
                bw.Write(value!.Value);
        }

        public static ulong? ReadNullableUInt64(this BinaryReader br)
        {
            return br.ReadBoolean() ? (ulong?) br.ReadUInt64() : null;
        }

        #endregion

        #region VarBuffer

        public static void WriteVarBuffer(this BinaryWriter bw, byte[]? buffer)
        {
            var hasBuffer = buffer != null;
            if (!bw.WriteBoolean(hasBuffer) || !hasBuffer)
                return;
            bw.Write(buffer!.Length);
            bw.Write(buffer);
        }

        public static byte[]? ReadVarBuffer(this BinaryReader br)
        {
            if (!br.ReadBoolean())
                return null;
            var length = br.ReadInt32();
            var buffer = br.ReadBytes(length);
            return buffer;
        }

        #endregion

        #region Guid

        public static void Write(this BinaryWriter bw, Guid value)
        {
            bw.Write(value.ToByteArray());
        }

        public static Guid ReadGuid(this BinaryReader br)
        {
            return new Guid(br.ReadBytes(16));
        }

        #endregion

        #region Concat

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Concat(this byte[] left, byte[] right) => Concat(left.AsSpan(), right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Concat(this byte[] left, ReadOnlySpan<byte> right) => Concat(left.AsSpan(), right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Concat(this ReadOnlySpan<byte> left, ReadOnlySpan<byte> right) => left.Concat<byte>(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char[] Concat(this ReadOnlySpan<char> left, ReadOnlySpan<char> right) => left.Concat<char>(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] Concat<T>(this ReadOnlySpan<T> left, ReadOnlySpan<T> right)
        {
            var result = new T[left.Length + right.Length];
            left.CopyTo(result);
            right.CopyTo(result.AsSpan().Slice(left.Length));
            return result;
        }

        #endregion
    }
}