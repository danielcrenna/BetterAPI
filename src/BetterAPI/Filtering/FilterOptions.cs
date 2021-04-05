namespace BetterAPI.Filtering
{
    public sealed class FilterOptions : IQueryOptions
    {
        public bool HasDefaultBehaviorWhenMissing { get; set; } = false;
        public bool EmptyClauseIsValid => false;
        public string Operator { get; set; } = "$filter";
    }
}
