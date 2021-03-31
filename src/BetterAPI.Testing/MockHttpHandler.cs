// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BetterAPI.Testing
{
    internal sealed class MockHttpHandler : DelegatingHandler
    {
        private readonly HttpRequestBuilder _builder;
        private readonly ILogger<MockHttpHandler>? _logger;

        public MockHttpHandler(HttpRequestBuilder builder, HttpMessageHandler innerHandler, ILogger<MockHttpHandler>? logger) : base(innerHandler)
        {
            _builder = builder;
            _logger = logger;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            var matched = _builder.TryMatch(request, out var response);

            response = !matched || response == default ? new HttpResponseMessage(HttpStatusCode.NotFound) : response;

            _logger?.LogInformation(matched? HttpEvents.MatchedMock : HttpEvents.UnmatchedMock,
                matched
                    ? "Matched HTTP request {Method} {RequestUri} after {Duration} - {StatusCode}"
                    : "Unmatched HTTP request {Method} {RequestUri} after {Duration} - {StatusCode}",
                request.Method, request.RequestUri, sw.Elapsed, (int) response.StatusCode);

            return Task.FromResult(response);
        }
    }
}