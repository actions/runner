using System.Collections.Generic;
using Microsoft.VisualStudio.Services.Graph;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace Microsoft.VisualStudio.Services.WebApi
{
    public static class VssJsonPatchDocumentFactory
    {
        public static JsonPatchDocument ConstructJsonPatchDocument(
            Operation operationType,
            string path,
            string value)
        {
            var patchDocument = new JsonPatchDocument();

            var operation = new JsonPatchOperation
            {
                Operation = operationType,
                Path = BuildPath(path),
                Value = value
            };

            patchDocument.Add(operation);

            return patchDocument;
        }

        public static JsonPatchDocument ConstructJsonPatchDocument(
            Operation operationType,
            IDictionary<string, object> dict)
        {
            var patchDocument = new JsonPatchDocument();

            foreach (var pair in dict)
            {
                var operation = new JsonPatchOperation
                {
                    Operation = operationType,
                    Path = BuildPath(pair.Key),
                    Value = pair.Value
                };

                patchDocument.Add(operation);
            }

            return patchDocument;
        }

        public static JsonPatchDocument ConstructJsonPatchDocument(
            Operation operationType,
            IEnumerable<string> paths)
        {
            var patchDocument = new JsonPatchDocument();

            foreach (var path in paths)
            {
                var operation = new JsonPatchOperation
                {
                    Operation = operationType,
                    Path = BuildPath(path)
                };

                patchDocument.Add(operation);
            }

            return patchDocument;
        }


        private static string BuildPath(string path)
        {
            return Constants.JsonPatchOperationPathPrefix + path;
        }
    }
}
