using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace BetterAPI.Logging
{
    public interface ILoggingStore
    {
        IEnumerable<LoggingEntry> Get(CancellationToken cancellationToken = default);
        IEnumerable<LoggingEntry> GetByLevel(LogLevel logLevel, CancellationToken cancellationToken = default);
        IEnumerable<LoggingEntry> GetByData(string key, CancellationToken cancellationToken = default);
    }
}