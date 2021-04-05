namespace BetterAPI.Shaping
{
    public sealed class IncludeOptions : IQueryOptions
    {
        public bool HasDefaultBehaviorWhenMissing { get; set; } = false;
        public bool EmptyClauseIsValid => false;
        public string Operator { get; set; } = "$include";
    }
}
