﻿namespace BetterAPI.Paging
{
    public sealed class TopOptions : IQueryOptions
    {
        public bool HasDefaultBehaviorWhenMissing { get; set; } = false;
        public bool EmptyClauseIsValid => false;
        public string Operator { get; set; } = "$top";
    }
}