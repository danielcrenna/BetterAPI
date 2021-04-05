using System.Collections.Generic;
using System.Threading;

namespace BetterAPI.Logging
{
    public interface ILoggingStore
    {
        IEnumerable<LoggingEntry> Get(CancellationToken cancellationToken = default);
        IEnumerable<LoggingEntry> GetByData(string key, CancellationToken cancellationToken = default);
        IEnumerable<LoggingEntry> GetByData(string key, string? value, CancellationToken cancellationToken = default);
    }
}