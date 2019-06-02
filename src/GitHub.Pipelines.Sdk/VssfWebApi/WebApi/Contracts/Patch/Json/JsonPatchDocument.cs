using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.WebApi.Patch.Json
{
    /// <summary>
    /// The JSON model for JSON Patch Operations
    /// </summary>
    [ClientIncludeModel]
    public class JsonPatchDocument : List<JsonPatchOperation>
    {
    }
}
