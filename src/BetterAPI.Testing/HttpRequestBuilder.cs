// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BetterAPI.Testing
{
    public class HttpRequestBuilder
    {
        private readonly IHttpClientBuilder _builder;
        private readonly Action<HttpRequestMessage> _configureRequest;
        private Action<HttpResponseMessage> _configureResponse;

        public HttpRequestBuilder(IHttpClientBuilder builder, Action<HttpRequestMessage> configureRequest)
        {
            _builder = builder;
            _configureRequest = configureRequest;
        }

        public void RespondWith(Action<HttpResponseMessage> configureResponse)
        {
            _configureResponse = configureResponse;
        }

        public bool TryMatch(HttpRequestMessage request, out HttpResponseMessage? response)
        {
            var exemplar = new HttpRequestMessage();
            _configureRequest?.Invoke(exemplar);

            if (exemplar.RequestUri == default)
            {
                response = default;
                return false;
            }

            if (exemplar.Method != request.Method)
            {
                response = default;
                return false;
            }

            response = new HttpResponseMessage();
            response.Headers.TryAddWithoutValidation(ApiHeaderNames.MockResponse, "true");
            _configureResponse?.Invoke(response);
            return true;
        }
    }
}