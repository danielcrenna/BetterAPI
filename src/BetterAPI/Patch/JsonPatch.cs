using System.Collections.Generic;

namespace BetterAPI.Patch
{
    public sealed class JsonPatch
    {
        public List<JsonPatchOperation> Operations { get; }

        public JsonPatch()
        {
            Operations = new List<JsonPatchOperation>();
        }
    }
}