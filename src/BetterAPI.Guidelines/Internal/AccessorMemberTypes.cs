using System;

namespace BetterAPI.Guidelines.Internal
{
    [Flags]
    public enum AccessorMemberTypes : byte
    {
        Fields = 1 << 1,
        Properties = 1 << 2,
        Methods = 1 << 3,

        None = 0x00,
        All = byte.MaxValue
    }
}