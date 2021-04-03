using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace BetterAPI.Testing
{
    partial class GivenASingleItemStore<TService, TModel, TStartup>
    {
        [Fact]
        public async Task Get_by_id_with_include_returns_result_with_only_included_field()
        {
            var client = _factory.CreateClientNoRedirects();
            var response = await client.GetAsync($"{_endpoint}/{Id}/?$include=Summary");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response.ShouldNotHaveHeader(ApiHeaderNames.PreferenceApplied);
            response.ShouldHaveHeader(HeaderNames.ETag);
            response.ShouldHaveContentHeader(HeaderNames.LastModified);
            
            var model = await response.Content.ReadFromJsonAsync<TModel>();
            Assert.NotNull(model ?? throw new NullReferenceException());
            
            Assert.NotEqual(Id, model.GetId());
        }

        [Fact]
        public async Task Get_by_id_with_exclude_returns_result_without_excluded_field()
        {
            var client = _factory.CreateClientNoRedirects();
            var response = await client.GetAsync($"{_endpoint}/{Id}/?$exclude=Summary");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response.ShouldNotHaveHeader(ApiHeaderNames.PreferenceApplied);
            response.ShouldHaveHeader(HeaderNames.ETag);
            response.ShouldHaveContentHeader(HeaderNames.LastModified);

            var model = await response.Content.ReadFromJsonAsync<TModel>();
            Assert.NotNull(model ?? throw new NullReferenceException());
            Assert.Equal(Id, model.GetId());
        }
    }
}
