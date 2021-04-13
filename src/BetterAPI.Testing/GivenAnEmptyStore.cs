// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;

namespace BetterAPI.Testing
{
    /// <summary>
    ///     <see href="https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-5.0" />
    /// </summary>
    public abstract partial class GivenAnEmptyStore<TModel, TStartup> : IClassFixture<WebApplicationFactory<TStartup>>
        where TStartup : class
    {
        private readonly string _endpoint;
        private readonly WebApplicationFactory<TStartup> _factory;

        protected GivenAnEmptyStore(string endpoint, ITestOutputHelper output, WebApplicationFactory<TStartup> factory)
        {
            _endpoint = endpoint;
            _factory = factory
                .WithoutLocalizationStartupService()
                .WithTestLogging(output);
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

            var body = await response.Content.ReadFromJsonAsync<Envelope<TModel>>() ??
                       throw new NullReferenceException();
            Assert.Empty(body.Value);
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

            var options = _factory.Services.GetRequiredService<IOptions<ProblemDetailsOptions>>();

            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(problemDetails);
            Assert.Equal((int) HttpStatusCode.BadRequest, problemDetails.Status);
            Assert.Equal("One or more validation errors occurred.", problemDetails.Title);
            Assert.Equal($"{options.Value.BaseUrl}{(int)HttpStatusCode.BadRequest}", problemDetails.Type);
        }
    }
}