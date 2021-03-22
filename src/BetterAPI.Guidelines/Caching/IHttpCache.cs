using System;

namespace BetterApi.Guidelines.Caching
{
    public interface IHttpCache
    {
        bool TryGetETag(string key, out string? etag);
        bool TryGetLastModified(string key, out DateTimeOffset lastModified);
        void Save(string key, string etag);
        void Save(string key, DateTimeOffset lastModified);
    }
}