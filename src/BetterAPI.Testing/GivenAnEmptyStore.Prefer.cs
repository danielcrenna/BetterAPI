using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace BetterAPI.Testing
{
    public abstract partial class GivenAnEmptyStore<TModel, TStartup>
    {
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

            var body = await response.Content.ReadFromJsonAsync<Envelope<TModel>>() ??
                       throw new NullReferenceException();
            Assert.Empty(body.Value);
        }

        [Fact]
        public async Task Get_with_minimal_preferred_returns_empty_body()
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
    }
}
