namespace BetterAPI.Paging
{
    public sealed class TopOptions : IQueryOptions
    {
        public bool EnabledByDefault { get; set; } = false;
        public bool EmptyClauseIsValid => false;
        public string Operator { get; set; } = "$top";
    }
}