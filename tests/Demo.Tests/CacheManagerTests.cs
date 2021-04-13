// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BetterAPI.Caching;
using BetterAPI.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Demo.Tests
{
    public class CacheControllerTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;

        public CacheControllerTests(ITestOutputHelper output, WebApplicationFactory<Startup> factory)
        {
            _factory = factory
                .WithoutLocalizationStartupService()
                .WithTestLogging(output);
        }

        [Fact]
        public async Task Can_get_cache_info()
        {
            var client = _factory.CreateClientNoRedirects();
            var response = await client.OptionsAsync("cache");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<CacheInfo>();
            Assert.Equal(0, body.KeyCount);
            Assert.Equal(0, body.SizeBytes);
        }
    }
}