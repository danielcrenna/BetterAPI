// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BetterAPI.Guidelines;
using BetterAPI.Guidelines.Reflection;
using Demo.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;

namespace Demo.Tests
{
    public abstract class GivenACollectionStore<TService, TModel> : IClassFixture<WebApplicationFactory<Startup>>
        where TService : class
    {
        private readonly string _endpoint;
        private readonly WebApplicationFactory<Startup> _factory;
        
        protected GivenACollectionStore(string endpoint, Action<TService> seeder, ITestOutputHelper output, WebApplicationFactory<Startup> factory)
        {
            _endpoint = endpoint;
            _factory = factory.WithTestLogging(output).WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<TService>();
                    services.AddSingleton(r =>
                    {
                        var service = Instancing.CreateInstance<TService>(ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services));
                        seeder(service);
                        return service;
                    });
                });
            });
        }

        [Fact]
        public async Task Get_returns_result()
        {
            var client = _factory.CreateClientNoRedirects();
            var response = await client.GetAsync($"{_endpoint}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response.ShouldNotHaveHeader(ApiHeaderNames.PreferenceApplied);
            response.ShouldHaveHeader(HeaderNames.ETag);
            response.ShouldHaveContentHeader(HeaderNames.LastModified);

            var model = await response.Content.ReadFromJsonAsync<IEnumerable<TModel>>();
            Assert.NotNull(model);
        } 
    }
}