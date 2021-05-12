// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace BetterAPI.Tokens
{
    public class TokenOptions
    {
        public string? Realm { get; set; }

        [Required]
        public string Issuer { get; set; }

        [Required]
        public string Audience { get; set; }

        [Required]
        public string? SigningKey { get; set; }

        [Required]
        public TokenFormat Format { get; set; }

        [Required]
        public TimeSpan Lifetime { get; set; }

        public TokenOptions()
        {
            Issuer = GetDefaultServerUrl();
            Audience = Issuer;
            Format = TokenFormat.JsonWebToken;
            Lifetime = TimeSpan.FromHours(1);
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