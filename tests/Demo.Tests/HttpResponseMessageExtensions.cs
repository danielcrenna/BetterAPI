// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Demo.Tests
{
    public static class HttpResponseMessageExtensions
    {
        public static HttpResponseMessage ShouldHaveHeader(this HttpResponseMessage response, string headerName)
        {
            Assert.True(response.Headers.Contains(headerName));
            return response;
        }

        public static HttpResponseMessage ShouldHaveContentHeader(this HttpResponseMessage response, string headerName)
        {
            Assert.True(response.Content.Headers.Contains(headerName));
            return response;
        }

        public static HttpResponseMessage ShouldNotHaveHeader(this HttpResponseMessage response, string headerName)
        {
            Assert.False(response.Headers.Contains(headerName));
            return response;
        }

        public static HttpResponseMessage ShouldNotHaveContentHeader(this HttpResponseMessage response, string headerName)
        {
            Assert.False(response.Content.Headers.Contains(headerName));
            return response;
        }

        public static void ShouldHaveValidDateHeader(this HttpResponseMessage response)
        {
             response.ShouldHaveHeader(HeaderNames.Date);

            var dateString = response.Headers.GetValues(HeaderNames.Date).SingleOrDefault();
            Assert.NotNull(dateString);
            Assert.NotEmpty(dateString ?? string.Empty);

            var provider = CultureInfo.InvariantCulture;
            var format = provider.DateTimeFormat.RFC1123Pattern;
            
            // Microsoft REST Guidelines specified https://tools.ietf.org/html/rfc5322#section-3.3
            // But RFC 1123 seems to point back to RFC 5322 or is otherwise identical
            //
            // Also guidelines specifies GMT (ala RFC 1123), and ignores the stipulations of RFC 5322:
            // 'The date and time-of-day SHOULD express local time.'
            Assert.True(DateTimeOffset.TryParseExact(dateString, format, provider, DateTimeStyles.AdjustToUniversal, out _));
        }
    }
}