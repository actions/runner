using System.Collections;
using System.ComponentModel;
using GitHub.Services.Common;
using GitHub.Services.WebApi.Patch.Json;

namespace GitHub.Services.WebApi.Patch
{
    /// <summary>
    /// Represents the JSON Patch Replace operation.
    /// </summary>
    /// <typeparam name="TModel">The model the patch operation applies to.</typeparam>
    public class ReplacePatchOperation<TModel> : PatchOperation<TModel>
    {
        public ReplacePatchOperation()
        {
            this.Operation = Operation.Replace;
        }

        public ReplacePatchOperation(string path, object value): this()
        {
            this.Path = path;
            this.Value = value;
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

            var value = ValidateAndGetValue(operation);
            if (value == null)
            {
                throw new VssPropertyValidationException("Value", PatchResources.ValueCannotBeNull());
            }

            return new ReplacePatchOperation<TModel>(operation.Path, value);
        }

        /// <summary>
        /// Applies the Replace patch operation to the target
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
                            list[index] = this.Value;
                        }
                        else
                        {
                            throw new PatchOperationFailedException(PatchResources.CannotReplaceNonExistantValue(this.Path));
                        }
                    }
                    else if (type.IsDictionary())
                    {
                        var dictionary = (IDictionary)parent;
                        if (!dictionary.Contains(current))
                        {
                            throw new InvalidPatchFieldNameException(PatchResources.InvalidFieldName(current));
                        }

                        dictionary[current] = this.Value;
                    }
                    else
                    {
                        if (type.GetMemberValue(current, parent) == null)
                        {
                            throw new PatchOperationFailedException(PatchResources.CannotReplaceNonExistantValue(this.Path));
                        }

                        type.SetMemberValue(current, parent, this.Value);
                    }
                });
        }
    }
}
