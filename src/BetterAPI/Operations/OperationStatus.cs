using System;

namespace BetterAPI.Operations
{
    public sealed class OperationStatus
    {
        public DateTimeOffset CreatedDateTime { get; set; }
        public DateTimeOffset LastActionDateTime { get; set; }
        public OperationState Status { get; set; }
        public float PercentComplete { get; set; }
        public string? ResourceLocation { get; set; }
    }
}