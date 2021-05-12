namespace BetterAPI.Patch
{
    public sealed class JsonPatchOperation
    {
        public JsonPatchOperationType Type { get; set; }
        public string? Path { get; set; }
    }
}