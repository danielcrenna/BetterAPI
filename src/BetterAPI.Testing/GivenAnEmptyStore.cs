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
using BetterAPI.Caching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace BetterAPI.Testing
{
    /// <summary>
    ///     <see href="https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-5.0" />
    /// </summary>
    public abstract class GivenAnEmptyStore<TModel, TStartup> : IClassFixture<WebApplicationFactory<TStartup>>
        where TStartup : class
    {
        private readonly string _endpoint;
        private readonly WebApplicationFactory<TStartup> _factory;

        protected GivenAnEmptyStore(string endpoint, ITestOutputHelper output, WebApplicationFactory<TStartup> factory)
        {
            _endpoint = endpoint;
            _factory = factory.WithTestLogging(output);
        }

        [Fact]
        public async Task Options_returns_allowed_methods()
        {
            var client = _factory.CreateClientNoRedirects();
            var response = await client.OptionsAsync(_endpoint);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Get_returns_empty_collection()
        {
            var client = _factory.CreateClientNoRedirects();

            var response = await client.GetAsync(_endpoint);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // we should not get this header if we didn't indicate a preference
            response.ShouldNotHaveContentHeader(ApiHeaderNames.PreferenceApplied);

            // we should have an ETag representing no results
            response.ShouldHaveHeader(HeaderNames.ETag);

            // there are no resources from which we can determine a modified date
            response.ShouldNotHaveContentHeader(HeaderNames.LastModified);

            var body = await response.Content.ReadFromJsonAsync<Envelope<TModel>>() ?? throw new NullReferenceException();
            Assert.Empty(body.Values);
        }

        [Fact]
        public async Task Get_with_representation_preferred_returns_empty_collection()
        {
            var client = _factory.CreateClientNoRedirects()
                .PreferRepresentation();

            var response = await client.GetAsync(_endpoint);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // we should get this header if we had a preference
            response.ShouldHaveHeader(ApiHeaderNames.PreferenceApplied);

            // we should have an ETag representing no results
            response.ShouldHaveHeader(HeaderNames.ETag);

            // there are no resources from which we can determine a modified date
            response.ShouldNotHaveContentHeader(HeaderNames.LastModified);

            var body = await response.Content.ReadFromJsonAsync<Envelope<TModel>>() ?? throw new NullReferenceException();
            Assert.Empty(body.Values);
        }

        [Fact]
        public async Task Get_with_minimal_response_preferred_returns_empty_body()
        {
            var client = _factory.CreateClientNoRedirects()
                .PreferMinimal();

            var response = await client.GetAsync(_endpoint);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // we should get this header if we had a preference
            response.ShouldHaveHeader(ApiHeaderNames.PreferenceApplied);

            // we should have an ETag representing no results
            response.ShouldHaveHeader(HeaderNames.ETag);

            // there are no resources from which we can determine a modified date
            response.ShouldNotHaveContentHeader(HeaderNames.LastModified);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Empty(body);
        }

        [Fact]
        public async Task Get_with_valid_if_none_match_returns_not_modified()
        {
            var client = _factory.CreateClientNoRedirects();
            client.DefaultRequestHeaders.TryAddWithoutValidation(HeaderNames.IfNoneMatch, ETag.WeakEmptyJsonArray);

            var response = await client.GetAsync(_endpoint);
            Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        }

        [Fact]
        public async Task Get_with_invalid_if_none_match_returns_result()
        {
            var client = _factory.CreateClientNoRedirects();
            client.DefaultRequestHeaders.TryAddWithoutValidation(HeaderNames.IfNoneMatch, ETag.InvalidWeak);

            var response = await client.GetAsync(_endpoint);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Get_with_invalid_if_match_returns_not_modified()
        {
            var client = _factory.CreateClientNoRedirects();
            client.DefaultRequestHeaders.TryAddWithoutValidation(HeaderNames.IfMatch, ETag.InvalidWeak);

            var response = await client.GetAsync(_endpoint);
            Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        }

        [Fact]
        public async Task Get_with_valid_if_match_returns_result()
        {
            var client = _factory.CreateClientNoRedirects();
            client.DefaultRequestHeaders.TryAddWithoutValidation(HeaderNames.IfMatch, ETag.WeakEmptyJsonArray);

            var response = await client.GetAsync(_endpoint);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Get_with_any_valid_id_returns_not_found()
        {
            var client = _factory.CreateClientNoRedirects();
            var response = await client.GetAsync($"{_endpoint}/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Get_with_invalid_id_returns_bad_request_with_details()
        {
            var client = _factory.CreateClientNoRedirects();
            var response = await client.GetAsync($"{_endpoint}/rosebud");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(problemDetails);
            Assert.Equal((int) HttpStatusCode.BadRequest, problemDetails.Status);
            Assert.Equal("One or more validation errors occurred.", problemDetails.Title);
            Assert.Equal("https://httpstatuscodes.com/400", problemDetails.Type);
        }
    }
}