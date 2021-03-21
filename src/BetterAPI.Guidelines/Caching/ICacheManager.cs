namespace BetterApi.Guidelines.Caching
{
    public interface ICacheManager
    {
        int KeyCount { get; }
        long? SizeLimitBytes { get; set; }
        long SizeBytes { get; set; }
    }
}