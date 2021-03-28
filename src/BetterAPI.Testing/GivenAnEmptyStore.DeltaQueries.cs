using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using BetterAPI.DeltaQueries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace BetterAPI.Testing
{
    public abstract partial class GivenAnEmptyStore<TModel, TStartup>
    {
        [Fact]
        public async Task Get_with_delta_operator_returns_empty_collection_with_delta_link()
        {
            var client = _factory.CreateClientNoRedirects();
            
            var response = await client.GetAsync($"{_endpoint}/?$delta");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // we should not get this header if we didn't indicate a preference
            response.ShouldNotHaveContentHeader(ApiHeaderNames.PreferenceApplied);

            // we should have an ETag representing no results
            response.ShouldHaveHeader(HeaderNames.ETag);

            // there are no resources from which we can determine a modified date
            response.ShouldNotHaveContentHeader(HeaderNames.LastModified);

            var json = await response.Content.ReadAsStringAsync();
            Assert.NotNull(json);
            Assert.NotEmpty(json);

            // i.e.: {"values":[],"@deltaLink":"..."}
            var options = _factory.Services.GetRequiredService<JsonSerializerOptions>();
            var body = await response.Content.ReadFromJsonAsync<DeltaAnnotated<Envelope<TModel>>>(options);
            Assert.NotNull(body.DeltaLink);
            Assert.NotEmpty(body.DeltaLink);
        }
    }
}
