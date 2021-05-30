using System;

namespace BetterAPI.Logging
{
    public sealed class LoggingEntryViewModel
    {
        public Guid Id { get; set; }
        public LoggingLevel? LogLevel { get; set; }
        public LoggingEvent? EventId { get; set; }
        public string? Message { get; set; }
    }
}
