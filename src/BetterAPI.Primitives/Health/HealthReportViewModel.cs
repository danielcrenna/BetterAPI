using System;
using System.Collections.Generic;

namespace BetterAPI.Health
{
    public sealed class HealthReportViewModel
    {
        public Dictionary<string, HealthReportEntryViewModel>? Entries { get; set; }
        public HealthStatus? Status { get; set;}
        public TimeSpan? TotalDuration { get; set;}
    }
}
