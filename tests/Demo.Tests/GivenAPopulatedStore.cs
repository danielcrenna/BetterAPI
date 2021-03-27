// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
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
    public abstract class GivenAPopulatedStore<TService> : IClassFixture<WebApplicationFactory<Startup>> 
        where TService : class
    {
        private readonly string _endpoint;
        private readonly WebApplicationFactory<Startup> _factory;
        protected Guid _id;

        protected GivenAPopulatedStore(string endpoint, Action<TService> seeder, ITestOutputHelper output, WebApplicationFactory<Startup> factory)
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
            });;
        }

        [Fact]
        public async Task Get_by_id_returns_result()
        {
            var client = _factory.CreateClientNoRedirects();
            var response = await client.GetAsync($"{_endpoint}/{_id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response.ShouldNotHaveHeader(ApiHeaderNames.PreferenceApplied);
            response.ShouldHaveHeader(HeaderNames.ETag);
            response.ShouldHaveContentHeader(HeaderNames.LastModified);

            var model = await response.Content.ReadFromJsonAsync<WeatherForecast>();
            Assert.NotNull(model);
            Assert.Equal(_id, model.Id);
        }

        [Fact]
        public async Task Get_by_id_with_minimal_preference_returns_empty_body()
        {
            var client = _factory.CreateClientNoRedirects()
                .PreferMinimal();

            var response = await client.GetAsync($"{_endpoint}/{_id}");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            response.ShouldHaveHeader(ApiHeaderNames.PreferenceApplied);

            // we should still have the headers for the representation
            response.ShouldHaveHeader(HeaderNames.ETag);
            response.ShouldHaveContentHeader(HeaderNames.LastModified);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Empty(body);
        }

        [Fact]
        public async Task Get_by_id_with_representation_preference_returns_result()
        {
            var client = _factory.CreateClientNoRedirects()
                .PreferRepresentation();

            var response = await client.GetAsync($"{_endpoint}/{_id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response.ShouldHaveHeader(ApiHeaderNames.PreferenceApplied);
            response.ShouldHaveHeader(HeaderNames.ETag);
            response.ShouldHaveContentHeader(HeaderNames.LastModified);

            var model = await response.Content.ReadFromJsonAsync<WeatherForecast>();
            Assert.NotNull(model);
            Assert.Equal(_id, model.Id);
        }

        [Fact]
        public async Task Get_by_id_with_random_id_returns_not_found()
        {
            var client = _factory.CreateClientNoRedirects();
            var response = await client.GetAsync($"{_endpoint}/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Get_by_id_with_invalid_if_none_match_returns_result()
        {
            var client = _factory.CreateClientNoRedirects();
            
            // use the invalid etag to request a result only if there are none matching it (and there is no match)
            client.DefaultRequestHeaders.TryAddWithoutValidation(HeaderNames.IfNoneMatch, "W/\"00000000000000000000000000000000\"");

            var response = await client.GetAsync($"{_endpoint}/{_id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Get_by_id_with_valid_if_none_match_returns_not_modified()
        {
            // get the etag that corresponds to this ID
            var client = _factory.CreateClientNoRedirects();
            var response = await client.GetAsync($"{_endpoint}/{_id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Headers.ETag);

            // use the etag to request a result only if there are none matching it (but there is a match)
            client.DefaultRequestHeaders.TryAddWithoutValidation(HeaderNames.IfNoneMatch, response.Headers.ETag.ToString());
            response = await client.GetAsync($"{_endpoint}/{_id}");
            Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        }
        
        [Fact]
        public async Task Get_by_id_with_invalid_if_match_returns_not_modified()
        {
            var client = _factory.CreateClientNoRedirects();

            // use the invalid etag to request a result only if there is a match for it (but there is no match)
            client.DefaultRequestHeaders.TryAddWithoutValidation(HeaderNames.IfMatch, "W/\"00000000000000000000000000000000\"");

            var response = await client.GetAsync($"{_endpoint}/{_id}");
            Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        }

        [Fact]
        public async Task Get_by_id_with_valid_if_match_returns_result()
        {
            // get the etag that corresponds to this ID
            var client = _factory.CreateClientNoRedirects();
            var response = await client.GetAsync($"{_endpoint}/{_id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Headers.ETag);

            // use the etag to request a result only if there is a match for it (and there is a match)
            client.DefaultRequestHeaders.TryAddWithoutValidation(HeaderNames.IfMatch, response.Headers.ETag.ToString());
            response = await client.GetAsync($"{_endpoint}/{_id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}