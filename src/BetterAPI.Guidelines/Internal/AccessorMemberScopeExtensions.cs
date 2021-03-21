namespace BetterAPI.Guidelines.Internal
{
    internal static class AccessorMemberScopeExtensions
    {
        public static bool HasFlagFast(this AccessorMemberScope value, AccessorMemberScope flag)
        {
            return (value & flag) != 0;
        }
    }
}