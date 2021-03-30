// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BetterAPI.Testing;
using BetterAPI.Tokens;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Demo.Tests
{
    public class TokenControllerTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;

        public TokenControllerTests(ITestOutputHelper output, WebApplicationFactory<Startup> factory)
        {
            _factory = factory.WithTestLogging(output);
        }

        [Fact]
        public async Task Invalid_token_request_returns_bad_request()
        {
            var client = _factory.CreateClientNoRedirects();
            var response = await client.PostAsJsonAsync("tokens", new TokenRequestModel { Identity = ""});
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Can_issue_token()
        {
            var client = _factory.CreateClientNoRedirects();
            var response = await client.PostAsJsonAsync("tokens", new TokenRequestModel { Identity = "bob@loblaw.com"});
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = (await response.Content.ReadFromJsonAsync<TokenResponseModel>()) ??
                       throw new NullReferenceException();

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(body.Token);
            var options = _factory.Services.GetRequiredService<IOptions<TokenOptions>>();
            
            Assert.Equal(options.Value.Issuer, token.Issuer);
            Assert.Equal(options.Value.Audience, token.Audiences.FirstOrDefault());
        }
    }
}