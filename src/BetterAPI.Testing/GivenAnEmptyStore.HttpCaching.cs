using System.Net;
using System.Threading.Tasks;
using BetterAPI.Caching;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace BetterAPI.Testing
{
    public abstract partial class GivenAnEmptyStore<TModel, TStartup>
    {
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
    }
}
