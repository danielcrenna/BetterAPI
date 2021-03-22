namespace BetterAPI.Guidelines.Caching
{
    public readonly struct ETag
    {
        public readonly ETagType Type;
        public readonly string Value;

        public ETag(ETagType type, string value)
        {
            Type = type;
            Value = value;
        }
    }
}