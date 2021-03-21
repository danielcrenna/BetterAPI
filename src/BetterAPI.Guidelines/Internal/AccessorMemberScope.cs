using System;

namespace BetterAPI.Guidelines.Internal
{
    [Flags]
    public enum AccessorMemberScope : byte
    {
        Public = 1 << 1,
        Private = 1 << 2,

        None = 0x00,
        All = byte.MaxValue
    }
}