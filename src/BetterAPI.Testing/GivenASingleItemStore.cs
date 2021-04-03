// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;

namespace BetterAPI.Testing
{
    public abstract partial class
        GivenASingleItemStore<TService, TModel, TStartup> : IClassFixture<WebApplicationFactory<TStartup>>
        where TService : class
        where TStartup : class
    {
        private readonly string _endpoint;
        private readonly WebApplicationFactory<TStartup> _factory;

        protected Guid Id;

        protected GivenASingleItemStore(string endpoint, Action<TService> seeder, ITestOutputHelper output,
            WebApplicationFactory<TStartup> factory)
        {
            _endpoint = endpoint;
            _factory = factory.WithTestLogging(output).WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<TService>();
                    services.AddSingleton(r =>
                    {
                        var service = Instancing.CreateInstance<TService>(services.BuildServiceProvider());
                        seeder(service);
                        return service;
                    });
                });
            });
        }

        [Fact]
        public async Task Get_by_id_returns_result()
        {
            var client = _factory.CreateClientNoRedirects();
            var response = await client.GetAsync($"{_endpoint}/{Id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response.ShouldNotHaveHeader(ApiHeaderNames.PreferenceApplied);
            response.ShouldHaveHeader(HeaderNames.ETag);
            response.ShouldHaveContentHeader(HeaderNames.LastModified);

            var model = await response.Content.ReadFromJsonAsync<TModel>();
            Assert.NotNull(model ?? throw new NullReferenceException());
            Assert.Equal(Id, model.GetId());
        }

        [Fact]
        public async Task Get_by_id_with_random_id_returns_not_found()
        {
            var client = _factory.CreateClientNoRedirects();
            var response = await client.GetAsync($"{_endpoint}/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}