namespace BetterAPI.Paging
{
    public sealed class SkipOptions : IQueryOptions
    {
        public bool HasDefaultBehaviorWhenMissing { get; set; } = false;
        public bool EmptyClauseIsValid => false;
        public string Operator { get; set; } = "$skip";
    }
}