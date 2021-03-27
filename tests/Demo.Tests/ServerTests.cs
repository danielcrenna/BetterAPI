// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BetterAPI.Guidelines;
using BetterAPI.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;

namespace Demo.Tests
{
    public class ServerTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;

        public ServerTests(ITestOutputHelper output, WebApplicationFactory<Startup> factory)
        {
            _factory = factory.WithTestLogging(output);
        }

        [Fact]
        public async Task Ping_returns_response()
        {
            var client = _factory.CreateClientNoRedirects();
            var response = await client.GetAsync("ping");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response.ShouldHaveValidDateHeader();
            response.ShouldHaveHeader(HeaderNames.Server);
            response.ShouldHaveHeader(HeaderNames.Connection);
            response.ShouldHaveContentHeader(HeaderNames.ContentLength);

            var options = _factory.Services.GetRequiredService<IOptions<ApiOptions>>();
            Assert.Equal(options.Value.ApiServer, response.Headers.GetValues(HeaderNames.Server).FirstOrDefault());
        }

        [Fact]
        public async Task Controller_route_produces_valid_RFC_1123_formatted_string()
        {
            var client = _factory.CreateClientNoRedirects();
            var response = await client.OptionsAsync("cache");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response.ShouldHaveValidDateHeader();
        }
    }
}