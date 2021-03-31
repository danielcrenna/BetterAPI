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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BetterAPI.Testing;
using BetterAPI.Tokens;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Xunit.Abstractions;

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
        public async Task Can_issue_jwt_token_with_valid_claims()
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
        
        [Fact]
        public async Task Can_issue_jwe_token_with_valid_claims()
        {
            var keyStore = new FakeEncryptionKeyStore();
            TokenOptions? options = default;

            var factory = _factory
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(services =>
                    {
                        services.Configure<TokenOptions>(o =>
                        {
                            o.Format = TokenFormat.JsonWebEncryption;
                            options = o; // HACK: we can't get options from DI without double-initializing
                        });
                        services.RemoveAll<IEncryptionKeyStore>();
                        services.AddSingleton<IEncryptionKeyStore>(keyStore);
                    });
                });

            var client = factory.CreateClientNoRedirects();

            var response = await client.PostAsJsonAsync("tokens", new TokenRequestModel { Identity = "bob@loblaw.com"});
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Confirm that we obtained the options from ConfigureTestServices
            Assert.NotNull(options ?? throw new NullReferenceException());
            
            var body = (await response.Content.ReadFromJsonAsync<TokenResponseModel>()) ?? throw new NullReferenceException();

            // Verify the controller used our fake key store
            Assert.NotNull(keyStore.TokenDecryptionKey);

            var handler = new JwtSecurityTokenHandler();

            // WTF: Calling this causes the TestServer to call services.AddTokens() again!
            // var options = factory.Services.GetRequiredService<IOptions<TokenOptions>>();

            handler.ValidateToken(body.Token, new TokenValidationParameters
            {
                ValidAudience = options.Audience,
                ValidIssuer = options.Issuer,
                RequireSignedTokens = false,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                TokenDecryptionKey = keyStore.TokenDecryptionKey,
            }, out SecurityToken token);

            Assert.True(token is JwtSecurityToken);
            var jwt = (JwtSecurityToken) token;

            Assert.Equal(options.Issuer, token.Issuer);
            Assert.Equal(options.Audience, jwt.Audiences.FirstOrDefault());
        }

        public sealed class FakeEncryptionKeyStore : IEncryptionKeyStore
        {
            public SymmetricSecurityKey? TokenDecryptionKey { get; private set; }

            public Task<EncryptingCredentials> GetCredentialsAsync(string? realm)
            {
                var buffer = new byte[32];
                using var random = RandomNumberGenerator.Create();
                random.GetBytes(buffer);
                var securityKey = new SymmetricSecurityKey(buffer);

                // FIXME: https://tools.ietf.org/html/rfc7518#section-5.1
                //        SecurityAlgorithms does not have an implementation of A256GCM, which is what we actually want to use here.
                var encrypting = new EncryptingCredentials(securityKey, SecurityAlgorithms.Aes256KeyWrap, SecurityAlgorithms.Aes256CbcHmacSha512);
                TokenDecryptionKey = securityKey;
                return Task.FromResult(encrypting);
            }
        }
    }
};