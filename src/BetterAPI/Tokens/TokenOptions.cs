// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Linq;
using System.Security.Cryptography;

namespace BetterAPI.Tokens
{
    public class TokenOptions
    {
        public string? Realm { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string SigningKey { get; set; }
        public TokenFormat Format { get; set; }
        
        public TokenOptions()
        {
            var buffer = new byte[32];
            using var random = RandomNumberGenerator.Create();
            random.GetBytes(buffer);

            SigningKey = Convert.ToBase64String(buffer);
            Issuer = GetDefaultServerUrl();
            Audience = Issuer;
        }

        /// <summary> Poll an environment variable to determine URLs, and choose the first HTTPS URL, if available. </summary>
        private static string GetDefaultServerUrl()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
            if (string.IsNullOrWhiteSpace(env))
                return "https://localhost";

            var urls = env.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            switch (urls.Length)
            {
                case 0:
                    return "https://localhost";
                case 1:
                    return urls[0];
            }

            var https = urls.FirstOrDefault(x => x.StartsWith("https"));
            return string.IsNullOrWhiteSpace(https) ? urls[0] : https;
        }
    }
}