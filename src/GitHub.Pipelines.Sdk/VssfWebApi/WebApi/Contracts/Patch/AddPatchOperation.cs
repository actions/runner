using System.Collections;
using System.ComponentModel;
using GitHub.Services.Common;
using GitHub.Services.WebApi.Patch.Json;

namespace GitHub.Services.WebApi.Patch
{
    /// <summary>
    /// Represents the JSON Patch Add operation.
    /// </summary>
    /// <typeparam name="TModel">The model the patch operation applies to.</typeparam>
    public class AddPatchOperation<TModel> : PatchOperation<TModel>
    {
        public AddPatchOperation()
        {
            this.Operation = Operation.Add;
        }

        public AddPatchOperation(string path, object value): this()
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

            return new AddPatchOperation<TModel>(operation.Path, value);
        }

        /// <summary>
        /// Applies the Add patch operation to the target
        /// </summary>
        /// <param name="target">The object to apply the operation to.</param>
        public override void Apply(TModel target)
        {
            this.Apply(
                target,
                (type, parent, current) =>
                {
                    // Empty current means replace the whole object.
                    if (string.IsNullOrEmpty(current))
                    {
                        parent = this.Value;
                    }
                    else if (type.IsList())
                    {
                        var list = (IList)parent;
                        if (current == EndOfIndex)
                        {
                            list.Add(this.Value);
                        }
                        else
                        {
                            int index;
                            // When index == list.Count it's the same
                            // as doing an index append to the end.
                            if (int.TryParse(current, out index) &&
                                list.Count >= index)
                            {
                                list.Insert(index, this.Value);
                            }
                            else
                            {
                                // We can't insert beyond the length of the list.
                                throw new PatchOperationFailedException(PatchResources.IndexOutOfRange(this.Path));
                            }
                        }
                    }
                    else if (type.IsDictionary())
                    {
                        ((IDictionary)parent)[current] = this.Value;
                    }
                    else
                    {
                        type.SetMemberValue(current, parent, this.Value);
                    }
                });
        }
    }
}
