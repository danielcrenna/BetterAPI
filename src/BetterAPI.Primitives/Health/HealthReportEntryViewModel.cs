using System;
using System.Collections.Generic;

namespace BetterAPI.Health
{
    public sealed class HealthReportEntryViewModel
    {
        public Dictionary<string, object>? Data { get; set; }
        public string? Description { get; set; }
        public TimeSpan? Duration { get; set; }
        public Exception? Exception { get; set; }
        public HealthStatus? Status { get; set; }
        public List<string>? Tags { get; set; }
    }
}