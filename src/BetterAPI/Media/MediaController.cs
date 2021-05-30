using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace BetterAPI.Media
{
    [Route("api/media")]
    public sealed class MediaController : Controller
    {
        [HttpOptions]
        public void Options()
        {
            Response.Headers.Add(HeaderNames.AcceptRanges, "bytes");
        }
    }
}
