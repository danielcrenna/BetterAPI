using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
        public IActionResult Get(CancellationToken cancellationToken)
        {
            var entries = _store.Get(cancellationToken);
            return Ok(new Envelope<LoggingEntry>(entries));
        }

        [HttpGet("{level}")]
        public IActionResult GetByLogLevel(LogLevel level, CancellationToken cancellationToken)
        {
            var entries = _store.GetByLevel(level, cancellationToken);
            return Ok(new Envelope<LoggingEntry>(entries));
        }
    }
}
