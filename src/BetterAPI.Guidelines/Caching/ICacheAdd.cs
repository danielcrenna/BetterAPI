using System;

namespace BetterAPI.Guidelines.Caching
{
    public interface ICacheAdd
    {
        bool Add(string key, object value);
        bool Add(string key, object value, DateTimeOffset absoluteExpiration);
        bool Add(string key, object value, TimeSpan slidingExpiration);
        bool Add(string key, object value, ICacheDependency dependency);
        bool Add(string key, object value, DateTimeOffset absoluteExpiration, ICacheDependency dependency);
        bool Add(string key, object value, TimeSpan slidingExpiration, ICacheDependency dependency);

        bool Add<T>(string key, T value);
        bool Add<T>(string key, T value, DateTimeOffset absoluteExpiration);
        bool Add<T>(string key, T value, TimeSpan slidingExpiration);
        bool Add<T>(string key, T value, ICacheDependency dependency);
        bool Add<T>(string key, T value, DateTimeOffset absoluteExpiration, ICacheDependency dependency);
        bool Add<T>(string key, T value, TimeSpan slidingExpiration, ICacheDependency dependency);
    }
}