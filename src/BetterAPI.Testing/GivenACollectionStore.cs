// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BetterAPI.Guidelines;
using BetterAPI.Guidelines.Reflection;
using BetterAPI.Guidelines.Sorting;
using BetterAPI.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;

namespace Demo.Tests
{
    public abstract class GivenACollectionStore<TService, TModel, TStartup> : IClassFixture<WebApplicationFactory<TStartup>>
        where TService : class where TStartup : class
    {
        private readonly string _endpoint;
        private readonly WebApplicationFactory<TStartup> _factory;

        /// <summary>
        /// Overrides should return the first ID inserted into the store, and the first ID should come second compared to the second ID.
        /// </summary>
        public abstract Guid IdGreaterThanInsertedFirst { get; }

        /// <summary>
        /// Overrides should return the second ID inserted into the store, and the second ID should come first compared to the first ID.
        /// </summary>
        public abstract Guid IdLessThanInsertedSecond { get; }
        
        protected GivenACollectionStore(string endpoint, ITestOutputHelper output, WebApplicationFactory<TStartup> factory)
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
                        Populate(service);
                        return service;
                    });
                });
            });
        }

        /// <summary>
        /// Overrides should return a sort clause that produces the reverse insertion order for the added items to the store.
        /// </summary>
        /// <returns></returns>
        public abstract SortClause AlternateSort();

        /// <summary>
        /// Overrides should call the provided service to populate the data store.
        /// </summary>
        /// <param name="service"></param>
        public abstract void Populate(TService service);

        [Fact]
        public async Task Get_without_order_by_returns_sorted_by_id_ascending()
        {
            var client = _factory.CreateClientNoRedirects();
            var response = await client.GetAsync($"{_endpoint}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response.ShouldNotHaveHeader(ApiHeaderNames.PreferenceApplied);
            response.ShouldHaveHeader(HeaderNames.ETag);
            response.ShouldHaveContentHeader(HeaderNames.LastModified);

            var model = await response.Content.ReadFromJsonAsync<IEnumerable<TModel>>();
            Assert.NotNull(model ?? throw new NullReferenceException());

            var ordered = model.ToList() ?? throw new NullReferenceException();
            Assert.Equal(2, ordered.Count);

            Assert.Equal(ordered[0]?.GetId(), IdLessThanInsertedSecond);
            Assert.Equal(ordered[1]?.GetId(), IdGreaterThanInsertedFirst);
        } 

        [Fact]
        public async Task Get_without_order_by_returns_custom_default_sort()
        {
            var client = _factory
                .WithWebHostBuilder(x =>
                {
                    x.ConfigureTestServices(services =>
                    {
                        services.Configure<SortOptions>(o =>
                        {
                            o.SortByDefault = true;
                            o.DefaultSort = new[] { AlternateSort() };
                        });
                    });
                })    
                .CreateClientNoRedirects();

            var response = await client.GetAsync($"{_endpoint}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response.ShouldNotHaveHeader(ApiHeaderNames.PreferenceApplied);
            response.ShouldHaveHeader(HeaderNames.ETag);
            response.ShouldHaveContentHeader(HeaderNames.LastModified);

            var model = await response.Content.ReadFromJsonAsync<IEnumerable<TModel>>();
            Assert.NotNull(model ?? throw new NullReferenceException());

            var ordered = model.ToList() ?? throw new NullReferenceException();
            Assert.Equal(2, ordered.Count);

            Assert.Equal(ordered[0]?.GetId(), IdLessThanInsertedSecond);
            Assert.Equal(ordered[1]?.GetId(), IdGreaterThanInsertedFirst);
        } 

        [Fact]
        public async Task Get_with_order_by_id_descending_returns_result()
        {
            var client = _factory.CreateClientNoRedirects();
            var response = await client.GetAsync($"{_endpoint}/?$orderBy=id desc");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response.ShouldNotHaveHeader(ApiHeaderNames.PreferenceApplied);
            response.ShouldHaveHeader(HeaderNames.ETag);
            response.ShouldHaveContentHeader(HeaderNames.LastModified);

            var model = await response.Content.ReadFromJsonAsync<IEnumerable<TModel>>();
            Assert.NotNull(model ?? throw new NullReferenceException());

            var ordered = model.ToList() ?? throw new NullReferenceException();
            Assert.Equal(2, ordered.Count);

            Assert.Equal(ordered[0]?.GetId(), IdGreaterThanInsertedFirst);
            Assert.Equal(ordered[1]?.GetId(), IdLessThanInsertedSecond);
        } 

        [Fact]
        public async Task Get_returns_insertion_order_when_sorting_by_default_is_disabled()
        {
            var client = _factory
                .WithWebHostBuilder(x =>
                {
                    x.ConfigureTestServices(services =>
                    {
                        services.Configure<SortOptions>(o => o.SortByDefault = false);
                    });
                })
                .CreateClientNoRedirects();

            var response = await client.GetAsync($"{_endpoint}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response.ShouldNotHaveHeader(ApiHeaderNames.PreferenceApplied);
            response.ShouldHaveHeader(HeaderNames.ETag);
            response.ShouldHaveContentHeader(HeaderNames.LastModified);

            var model = await response.Content.ReadFromJsonAsync<IEnumerable<TModel>>();
            Assert.NotNull(model ?? throw new NullReferenceException());

            var ordered = model.ToList() ?? throw new NullReferenceException();
            Assert.Equal(2, ordered.Count);

            Assert.Equal(ordered[0]?.GetId(), IdGreaterThanInsertedFirst);
            Assert.Equal(ordered[1]?.GetId(), IdLessThanInsertedSecond);
        } 
    }
}