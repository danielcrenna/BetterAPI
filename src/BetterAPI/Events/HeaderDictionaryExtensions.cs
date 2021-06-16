// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using BetterAPI.Reflection;
using Microsoft.AspNetCore.Http;

namespace BetterAPI.Events
{
    internal static class HeaderDictionaryExtensions
    {
        public static bool TryGetHeaderString(this IHeaderDictionary headers, out string? headersString)
        {
            var sb = Pooling.StringBuilderPool.Get();
            try
            {
                var count = 0;
                foreach (var (key, value) in headers)
                {
                    if (key.StartsWith(":"))
                        continue; // ignore HTTP/2 stream pseudo-headers

                    sb.Append(key);
                    sb.Append(':');
                    sb.Append(' ');
                    sb.Append(value);
                    count++;
                    if(count < headers.Count)
                        sb.AppendLine();
                }

                if (sb.Length == 0)
                {
                    headersString = default;
                    return false;
                }

                headersString = sb.ToString();
                return true;
            }
            finally
            {
                Pooling.StringBuilderPool.Return(sb);
            }
        }
    }
}