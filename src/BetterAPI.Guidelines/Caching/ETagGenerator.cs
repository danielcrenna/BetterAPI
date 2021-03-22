using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace BetterAPI.Guidelines.Caching
{
    internal static class ETagGenerator
    {
        public static ETag Generate(ReadOnlySpan<byte> buffer)
        {
            using var md5 = MD5.Create();
            var hash = new byte[md5.HashSize/ 8];
            var hashed = md5.TryComputeHash(buffer, hash, out _);
            Debug.Assert(hashed);
            var hex = BitConverter.ToString(hash);
            return new ETag(ETagType.Weak, $"W/\"{hex.Replace("-", "")}\"");
        }
    }
}
