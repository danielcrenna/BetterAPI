// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace BetterAPI.Cryptography
{
    public static class Crypto
    {
        public const uint PublicKeyBytes = 32U;
        public const uint SecretKeyBytes = 64U;
        public const uint EncryptionKeyBytes = 32U;

        private static int _initialized;

        public static void Initialize()
        {
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) == 0)
                unsafe
                {
                    if(NativeMethods.sodium_init() != 0)
                        throw new CryptographicException();

                    NativeMethods.sodium_free(NativeMethods.sodium_malloc(0));
                }
        }

        #region Utilities

        public static void FillNonZeroBytes(Span<byte> buffer)
        {
            unsafe
            {
                fixed (byte* b = &buffer.GetPinnableReference())
                {
                    NativeMethods.randombytes_buf(b, (uint)buffer.Length);
                }
            }
        }

        public static void FillNonZeroBytes(Span<byte> buffer, uint size)
        {
            unsafe
            {
                fixed (byte* b = &buffer.GetPinnableReference())
                {
                    NativeMethods.randombytes_buf(b, size);
                }
            }
        }

        public static byte[] ToBinary(this string hexString)
        {
            var buffer = new byte[hexString.Length >> 1];
            var span = buffer.AsSpan();
            ToBinary(hexString, ref span);
            return buffer;
        }

        public static void ToBinary(this string hexString, ref Span<byte> buffer)
        {
            var length = ToBinary(Encoding.UTF8.GetBytes(hexString), buffer);
            if (length < buffer.Length)
                buffer = buffer.Slice(0, length);
        }

        public static int ToBinary(this ReadOnlySpan<byte> hexString, Span<byte> buffer)
        {
            var binMaxLen = buffer.Length;
            var hexLen = hexString.Length;
            unsafe
            {
                fixed (byte* bin = &buffer.GetPinnableReference())
                fixed (byte* hex = &hexString.GetPinnableReference())
                {
                    if (NativeMethods.sodium_hex2bin(bin, binMaxLen, hex, hexLen, null, out var binLen, null) != 0)
                        throw new InvalidOperationException(nameof(NativeMethods.sodium_hex2bin));

                    return binLen;
                }
            }
        }

        public static string? ToHexString(this ReadOnlySpan<byte> bin)
        {
            return ToHexString(bin, new Span<byte>(new byte[bin.Length * 2 + 1]));
        }

        public static string? ToHexString(this ReadOnlySpan<byte> bin, Span<byte> hex)
        {
            var minLength = bin.Length * 2 + 1;
            if (hex.Length < minLength)
                throw new ArgumentOutOfRangeException(nameof(hex), hex.Length,
                    $"Hex buffer is shorter than {minLength}");

            unsafe
            {
                fixed (byte* h = &hex.GetPinnableReference())
                fixed (byte* b = &bin.GetPinnableReference())
                {
                    var ptr = NativeMethods.sodium_bin2hex(h, hex.Length, b, bin.Length);
                    return Marshal.PtrToStringAnsi(ptr);
                }
            }
        }

        #endregion

        #region Public-key Cryptography (Ed25519)

        public static unsafe void GenerateKeyPair(out byte[] publicKey, out byte* secretKey)
        {
            publicKey = new byte[PublicKeyBytes];
            var sk = (byte*)NativeMethods.sodium_malloc(SecretKeyBytes);
            fixed (byte* pk = publicKey)
            {
                if (NativeMethods.crypto_sign_keypair(pk, sk) != 0)
                    throw new InvalidOperationException(nameof(NativeMethods.crypto_sign_keypair));
                secretKey = sk;
            }
        }

        public static unsafe ulong SignDetached(string message, byte* sk, Span<byte> signature)
        {
            return SignDetached(Encoding.UTF8.GetBytes(message), sk, signature);
        }

        public static unsafe ulong SignDetached(ReadOnlySpan<byte> message, byte* sk, Span<byte> signature)
        {
            var length = 0UL;

            fixed (byte* sig = &signature.GetPinnableReference())
            fixed (byte* m = &message.GetPinnableReference())
            {
                if (NativeMethods.crypto_sign_detached(sig, ref length, m, (ulong)message.Length, sk) != 0)
                    throw new InvalidOperationException(nameof(NativeMethods.crypto_sign_detached));
            }

            return length;
        }

        public static bool VerifyDetached(string message, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> publicKey)
        {
            return VerifyDetached(Encoding.UTF8.GetBytes(message), signature, publicKey);
        }

        public static bool VerifyDetached(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature,
            ReadOnlySpan<byte> publicKey)
        {
            unsafe
            {
                fixed (byte* sig = &signature.GetPinnableReference())
                fixed (byte* m = &message.GetPinnableReference())
                fixed (byte* pk = &publicKey.GetPinnableReference())
                {
                    var result = NativeMethods.crypto_sign_verify_detached(sig, m, (ulong)message.Length, pk);
                    return result == 0;
                }
            }
        }

        #endregion

        #region Hashing
        
        public static byte[] Hash(this ReadOnlySpan<byte> publicKey, ReadOnlySpan<byte> value)
        {
            var buffer = new byte[PublicKeyBytes];

            unsafe
            {
                fixed (byte* pk = publicKey)
                fixed (byte* id = buffer)
                fixed (byte* key = value)
                {
                    if (NativeMethods.crypto_generichash(id, buffer.Length, pk, PublicKeyBytes, key, value.Length) != 0)
                        throw new InvalidOperationException(nameof(NativeMethods.crypto_generichash));
                }

                return buffer;
            }
        }

        #endregion
    }
}