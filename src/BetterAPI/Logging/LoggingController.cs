using Microsoft.AspNetCore.Mvc;

namespace BetterAPI.Logging
{
    [Route("api/logs")]
    public sealed class LoggingController : Controller
    {
        private readonly ILoggingStore _store;

        public LoggingController(ILoggingStore store)
        {
            _store = store;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var entries = _store.Get();
            return Ok(new Envelope<LoggingEntry>(entries));
        }
    }
}
