// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

// ReSharper disable IdentifierTypo
// ReSharper disable UnusedMember.Global

namespace BetterAPI.Cryptography
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Binding")]
    internal static class NativeMethods
    {
        public const string DllName = "libsodium";

        /// <summary>
        ///     <see href="https://libsodium.gitbook.io/doc/generating_random_data" />
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void randombytes_buf(byte* buf, uint size);

        /// <summary>
        ///     <see href="https://libsodium.gitbook.io/doc/usage" />
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int sodium_init();

        /// <summary>
        ///     <see href="https://libsodium.gitbook.io/doc/memory_management#guarded-heap-allocations" />
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void sodium_free(void* ptr);

        /// <summary>
        ///     <see href="https://libsodium.gitbook.io/doc/memory_management#guarded-heap-allocations" />
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void* sodium_malloc(ulong size);

        /// <summary>
        ///     <see href="https://libsodium.gitbook.io/doc/public-key_cryptography/public-key_signatures" />
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int crypto_sign_keypair(byte* pk, byte* sk);

        /// <summary>
        ///     <see href="https://libsodium.gitbook.io/doc/public-key_cryptography/public-key_signatures#detached-mode" />
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int crypto_sign_detached(byte* sig, ref ulong siglen, byte* m, ulong mlen, byte* sk);

        /// <summary>
        ///     <see href="https://libsodium.gitbook.io/doc/public-key_cryptography/public-key_signatures#detached-mode" />
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int crypto_sign_verify_detached(byte* sig, byte* m, ulong mlen, byte* pk);

        /// <summary>
        ///     <see href="https://libsodium.gitbook.io/doc/helpers#hexadecimal-encoding-decoding" />
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe IntPtr sodium_bin2hex(byte* hex, int hexMaxlen, byte* bin, int binLen);

        /// <summary>
        ///     <see href="https://libsodium.gitbook.io/doc/helpers#hexadecimal-encoding-decoding" />
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe IntPtr sodium_hex2bin(byte* bin, int binMaxLen, byte* hex, int hexLen);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int sodium_hex2bin(byte* bin, int binMaxlen, byte* hex, int hexLen, string? ignore,
            out int binLen, string? hexEnd);

        /// <summary>
        ///     <see href="https://libsodium.gitbook.io/doc/hashing/generic_hashing" />
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int crypto_generichash(byte* @out, int outlen, byte* @in, ulong inlen, byte* key,
            int keylen);
    }
}
