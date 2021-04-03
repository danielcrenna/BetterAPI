namespace BetterAPI.Shaping
{
    public sealed class IncludeOptions : IQueryOptions
    {
        public bool EnabledByDefault { get; set; } = true;
        public string Operator { get; set; } = "$include";
    }
}
