﻿namespace BetterAPI.Filtering
{
    public sealed class FilterOptions : IQueryOptions
    {
        public bool EnabledByDefault { get; set; } = false;
        public bool EmptyClauseIsValid => false;
        public string Operator { get; set; } = "$filter";
    }
}
