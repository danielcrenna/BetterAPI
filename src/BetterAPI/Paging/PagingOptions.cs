namespace BetterAPI.Paging
{
    public sealed class PagingOptions
    {
        public TopOptions Top { get; set; } = new TopOptions();
        public SkipOptions Skip { get; set; } = new SkipOptions();
        public MaxPageSizeOptions MaxPageSize { get; set; } = new MaxPageSizeOptions();
        public CountOptions Count { get; set; } = new CountOptions();
        public bool AppendPagingLinkRelations { get; set; } = true;
    }
}