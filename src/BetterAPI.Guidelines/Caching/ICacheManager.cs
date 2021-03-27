using System.Collections.Generic;

namespace BetterAPI.Guidelines.Caching
{
    public interface ICacheManager : ICacheInfo
    {
        IEnumerable<string> IntrospectKeys();
    }
}