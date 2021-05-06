namespace BetterAPI.Search
{
    public sealed class SearchOptions : IQueryOptions
    {
        public bool HasDefaultBehaviorWhenMissing { get; set; } = false;
        public bool EmptyClauseIsValid => false;
        public string Operator { get; set; } = "$query";
    }
}
