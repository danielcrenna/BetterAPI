using System.Net;
using System.Threading.Tasks;
using BetterAPI.Caching;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace BetterAPI.Testing
{
    partial class GivenASingleItemStore<TService, TModel, TStartup>
    {
        [Fact]
        public async Task Get_by_id_with_invalid_if_none_match_returns_result()
        {
            var client = _factory.CreateClientNoRedirects();

            // use the invalid etag to request a result only if there are none matching it (and there is no match)
            client.DefaultRequestHeaders.TryAddWithoutValidation(HeaderNames.IfNoneMatch, ETag.InvalidWeak);

            var response = await client.GetAsync($"{_endpoint}/{Id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Get_by_id_with_valid_if_none_match_returns_not_modified()
        {
            // get the etag that corresponds to this ID
            var client = _factory.CreateClientNoRedirects();
            var response = await client.GetAsync($"{_endpoint}/{Id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Headers.ETag);

            // use the etag to request a result only if there are none matching it (but there is a match)
            client.DefaultRequestHeaders.TryAddWithoutValidation(HeaderNames.IfNoneMatch, response.Headers.ETag?.ToString());
            response = await client.GetAsync($"{_endpoint}/{Id}");
            Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        }

        [Fact]
        public async Task Get_by_id_with_invalid_if_match_returns_not_modified()
        {
            var client = _factory.CreateClientNoRedirects();

            // use the invalid etag to request a result only if there is a match for it (but there is no match)
            client.DefaultRequestHeaders.TryAddWithoutValidation(HeaderNames.IfMatch, ETag.InvalidWeak);

            var response = await client.GetAsync($"{_endpoint}/{Id}");
            Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        }

        [Fact]
        public async Task Get_by_id_with_valid_if_match_returns_result()
        {
            // get the etag that corresponds to this ID
            var client = _factory.CreateClientNoRedirects();
            var response = await client.GetAsync($"{_endpoint}/{Id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Headers.ETag);

            // use the etag to request a result only if there is a match for it (and there is a match)
            client.DefaultRequestHeaders.TryAddWithoutValidation(HeaderNames.IfMatch, response.Headers.ETag?.ToString());
            response = await client.GetAsync($"{_endpoint}/{Id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
