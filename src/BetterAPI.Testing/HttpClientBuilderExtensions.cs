// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BetterAPI.Testing
{
    public static class HttpClientBuilderExtensions
    {
        public static HttpRequestBuilder WhenRequestMatches(this IHttpClientBuilder builder, Action<HttpRequestMessage> configureAction)
        {
            var requestBuilder = new HttpRequestBuilder(builder, configureAction);
            builder.ConfigurePrimaryHttpMessageHandler(r => new MockHttpHandler(requestBuilder, new HttpClientHandler(), r.GetService<ILogger<MockHttpHandler>>()));
            return requestBuilder;
        }
    }
}