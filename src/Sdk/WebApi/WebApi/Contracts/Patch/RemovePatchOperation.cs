using System.Collections;
using System.ComponentModel;
using GitHub.Services.Common;
using GitHub.Services.WebApi.Patch.Json;

namespace GitHub.Services.WebApi.Patch
{
    /// <summary>
    /// Represents the JSON Patch Remove operation.
    /// </summary>
    /// <typeparam name="TModel">The model the patch operation applies to.</typeparam>
    public class RemovePatchOperation<TModel> : PatchOperation<TModel>
    {
        public RemovePatchOperation()
        {
            this.Operation = Operation.Remove;
        }

        public RemovePatchOperation(string path) : this()
        {
            this.Path = path;
        }

        /// <summary>
        /// Creates the strongly typed PatchOperation and validates the operation.
        /// </summary>
        /// <param name="operation">The simple json patch operation model.</param>
        /// <returns>A valid and strongly typed PatchOperation.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static new PatchOperation<TModel> CreateFromJson(JsonPatchOperation operation)
        {
            ValidatePath(operation);
            ValidateType(operation);

            if (operation.Value != null)
            {
                throw new VssPropertyValidationException("Value", PatchResources.ValueNotNull());
            }

            return new RemovePatchOperation<TModel>(operation.Path);
        }

        /// <summary>
        /// Applies the Remove patch operation to the target
        /// </summary>
        /// <param name="target">The object to apply the operation to.</param>
        public override void Apply(TModel target)
        {
            this.Apply(
                target,
                (type, parent, current) =>
                {
                    if (type.IsList())
                    {
                        var list = (IList)parent;
                        int index;
                        if (int.TryParse(current, out index) &&
                            list.Count > index)
                        {
                            list.RemoveAt(index);
                        }
                        else
                        {
                            // We can't remove outside the rangeof the list.
                            throw new PatchOperationFailedException(PatchResources.IndexOutOfRange(this.Path));
                        }
                    }
                    else if (type.IsDictionary())
                    {
                        ((IDictionary)parent).Remove(current);
                    }
                    else
                    {
                        type.SetMemberValue(current, parent, this.Value);
                    }
                });
        }
    }
}
