using System;
using Microsoft.Extensions.Primitives;

namespace BetterAPI.Guidelines.Caching
{
    public interface ICacheDependency : IDisposable
    {
        IChangeToken GetChangeToken();
    }
}