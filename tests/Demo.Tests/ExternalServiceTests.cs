// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BetterAPI.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Demo.Tests
{
    public class ExternalServiceTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;

        public ExternalServiceTests(ITestOutputHelper output, WebApplicationFactory<Startup> factory)
        {
            _factory = factory.WithTestLogging(output);
        }

        [Fact]
        public async Task Can_call_external_service_with_raw_file_isolation()
        {
            var factory = _factory
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(services =>
                    {
                        var b = services.AddHttpClient("github", c =>
                        {
                            c.BaseAddress = new Uri("https://api.github.com/");
                            c.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
                            c.DefaultRequestHeaders.Add("User-Agent", "ExternalServiceTests");
                        });
                        b.WhenRequestMatches(x =>
                        {
                            x.Method = HttpMethod.Get;
                            x.RequestUri = new Uri("/", UriKind.Relative);
                        })
                        .RespondWith(x =>
                        {
                            x.StatusCode = HttpStatusCode.OK;
                            x.Content = new StringContent(File.ReadAllText("InputFiles\\github_raw_response.json", Encoding.UTF8));
                        });
                    });
                });
            
            var httpFactory = factory.Services.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpFactory.CreateClient("github");
            
            var response = await httpClient.GetAsync("/");
            response.ShouldBeMock();
            response.EnsureSuccessStatusCode();
        }
    }
}