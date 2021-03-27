using Microsoft.AspNetCore.Mvc;

namespace BetterAPI.Guidelines.Caching
{
    [Route("cache")]
    [DoNotHttpCache]
    public sealed class CacheController : Controller
    {
        private readonly ICacheManager _cacheManager;

        public CacheController(ICacheManager cacheManager)
        {
            _cacheManager = cacheManager;
        }

        [HttpOptions]
        public IActionResult GetUsageInfo()
        {
            return new ObjectResult(_cacheManager);
        }

        [HttpGet("keys")]
        public IActionResult GetKeys()
        {
            return new ObjectResult(_cacheManager.IntrospectKeys());
        }
    }
}
