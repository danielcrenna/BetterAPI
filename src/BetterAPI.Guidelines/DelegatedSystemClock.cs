using System;
using Microsoft.Extensions.Internal;

namespace BetterAPI.Guidelines
{
    internal sealed class DelegatedSystemClock : ISystemClock
    {
        private readonly Func<DateTimeOffset> _timestamps;

        public DelegatedSystemClock(Func<DateTimeOffset> timestamps) => _timestamps = timestamps;

        public DateTimeOffset UtcNow => _timestamps().ToUniversalTime();
    }
}
