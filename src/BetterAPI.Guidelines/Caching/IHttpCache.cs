using System;

namespace BetterAPI.Guidelines.Caching
{
    public interface IHttpCache
    {
        bool TryGetETag(string cacheKey, out string? etag);
        bool TryGetLastModified(string cacheKey, out DateTimeOffset lastModified);
        void Save(string displayUrl, string etag);
        void Save(string displayUrl, DateTimeOffset lastModified);
    }
}