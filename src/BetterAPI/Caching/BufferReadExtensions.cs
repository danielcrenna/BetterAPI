// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace BetterAPI.Caching
{
    public static class BufferReadExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadString(this ReadOnlySpan<byte> buffer, int offset)
        {
            if (!ReadBoolean(buffer, offset)) return null;

            var length = ReadInt32(buffer, offset + 1);
            var sliced = buffer.Slice(offset + 1 + sizeof(int), length);

            unsafe
            {
                fixed (byte* b = sliced)
                {
                    return Encoding.UTF8.GetString(b, sliced.Length);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadBoolean(this ReadOnlySpan<byte> buffer, int offset)
        {
            return buffer[offset] != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32(this ReadOnlySpan<byte> buffer, int offset)
        {
            unsafe
            {
                fixed (byte* ptr = &buffer.GetPinnableReference())
                {
                    return *(int*) (ptr + offset);
                }
            }
        }
    }
}