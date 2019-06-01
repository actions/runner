using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace Microsoft.VisualStudio.Services.WebApi.Patch
{
    public class PatchDocument<TModel> : IPatchDocument<TModel>
    {
        public IEnumerable<IPatchOperation<TModel>> Operations { get; set; }

        /// <summary>
        /// Event fired before applying a patch operation.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public event PatchOperationApplyingEventHandler PatchOperationApplying;

        /// <summary>
        /// Event fired after a patch operation has been applied.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public event PatchOperationAppliedEventHandler PatchOperationApplied;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Apply(TModel target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            if (this.Operations == null)
            {
                throw new VssPropertyValidationException("Operations", PatchResources.NullOrEmptyOperations());
            }

            foreach (var operation in this.Operations)
            {
                // Set the events for the operations
                if (PatchOperationApplying != null)
                {
                    operation.PatchOperationApplying += PatchOperationApplying;
                }

                if (PatchOperationApplied != null)
                {
                    operation.PatchOperationApplied += PatchOperationApplied;
                }
                

                operation.Apply(target);

                // Clear the events for the operations
                if (PatchOperationApplying != null)
                {
                    operation.PatchOperationApplying -= PatchOperationApplying;
                }

                if (PatchOperationApplied != null)
                {
                    operation.PatchOperationApplied -= PatchOperationApplied;
                }
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static PatchDocument<TModel> CreateFromJson(JsonPatchDocument jsonPatchDocument)
        {
            if (jsonPatchDocument == null)
            {
                throw new VssPropertyValidationException("JsonPatch", PatchResources.JsonPatchNull());
            }

            // It's possible to put a null item into a list, so check for the list not being
            // empty AND none of the elements are null.
            if (!jsonPatchDocument.Any() || jsonPatchDocument.Any(d => d == null))
            {
                throw new VssPropertyValidationException("Operations", PatchResources.NullOrEmptyOperations());
            }

            var document = new PatchDocument<TModel>();
            var operations = new List<PatchOperation<TModel>>();
            foreach (var operation in jsonPatchDocument)
            {
                operations.Add(PatchOperation<TModel>.CreateFromJson(operation));
            }

            // Only set if there are operations to apply.
            if (operations.Any())
            {
                document.Operations = operations;
            }

            return document;
        }
    }
}
