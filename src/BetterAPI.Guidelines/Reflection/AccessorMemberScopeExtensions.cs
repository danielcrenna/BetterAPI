namespace BetterAPI.Guidelines.Reflection
{
    internal static class AccessorMemberScopeExtensions
    {
        public static bool HasFlagFast(this AccessorMemberScope value, AccessorMemberScope flag)
        {
            return (value & flag) != 0;
        }
    }
}