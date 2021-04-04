namespace BetterAPI.Shaping
{
    public sealed class IncludeOptions : IQueryOptions
    {
        public bool EnabledByDefault { get; set; } = false;
        public bool EmptyClauseIsValid => false;
        public string Operator { get; set; } = "$include";
    }
}
