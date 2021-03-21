using System;

namespace BetterAPI.Guidelines.Caching
{
    public interface ICacheSet
    {
        bool Set(string key, object value);
        bool Set(string key, object value, DateTimeOffset absoluteExpiration);
        bool Set(string key, object value, TimeSpan slidingExpiration);
        bool Set(string key, object value, ICacheDependency dependency);
        bool Set(string key, object value, DateTimeOffset absoluteExpiration, ICacheDependency dependency);
        bool Set(string key, object value, TimeSpan slidingExpiration, ICacheDependency dependency);

        bool Set<T>(string key, T value);
        bool Set<T>(string key, T value, DateTimeOffset absoluteExpiration);
        bool Set<T>(string key, T value, TimeSpan slidingExpiration);
        bool Set<T>(string key, T value, ICacheDependency dependency);
        bool Set<T>(string key, T value, DateTimeOffset absoluteExpiration, ICacheDependency dependency);
        bool Set<T>(string key, T value, TimeSpan slidingExpiration, ICacheDependency dependency);
    }
}