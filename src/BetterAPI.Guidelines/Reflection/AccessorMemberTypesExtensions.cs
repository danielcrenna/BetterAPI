namespace BetterAPI.Guidelines.Reflection
{
    internal static class AccessorMemberTypesExtensions
    {
        public static bool HasFlagFast(this AccessorMemberTypes value, AccessorMemberTypes flag)
        {
            return (value & flag) != 0;
        }
    }
}