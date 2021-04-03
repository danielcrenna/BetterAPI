using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace BetterAPI.Testing
{
    partial class GivenASingleItemStore<TService, TModel, TStartup>
    {
        [Fact]
        public async Task Get_by_id_with_minimal_preference_returns_empty_body()
        {
            var client = _factory.CreateClientNoRedirects()
                .PreferMinimal();

            var response = await client.GetAsync($"{_endpoint}/{Id}");
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

            var response = await client.GetAsync($"{_endpoint}/{Id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response.ShouldHaveHeader(ApiHeaderNames.PreferenceApplied);
            response.ShouldHaveHeader(HeaderNames.ETag);
            response.ShouldHaveContentHeader(HeaderNames.LastModified);

            var model = await response.Content.ReadFromJsonAsync<TModel>();
            Assert.NotNull(model ?? throw new NullReferenceException());
            Assert.Equal(Id, model.GetId());
        }
    }
}
