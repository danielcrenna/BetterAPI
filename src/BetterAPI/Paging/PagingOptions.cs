﻿namespace BetterAPI.Paging
{
    public sealed class PagingOptions
    {
        public TopOptions Top { get; set; } = new TopOptions();
        public SkipOptions Skip { get; set; } = new SkipOptions();
    }
}