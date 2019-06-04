using System.Collections;
using GitHub.Services.Common;
using GitHub.Services.WebApi.Patch.Json;

namespace GitHub.Services.WebApi.Patch
{
    /// <summary>
    /// Represents the JSON Patch Test operation.
    /// </summary>
    /// <typeparam name="TModel">The model the patch operation applies to.</typeparam>
    public class TestPatchOperation<TModel> : PatchOperation<TModel>
    { 
        public TestPatchOperation()
        {
            this.Operation = Operation.Test;
        }

        public TestPatchOperation(string path, object value): this()
        {
            this.Path = path;
            this.Value = value;
        }

        /// <summary>
        /// Creates the strongly typed PatchOperation and validates the operation.
        /// </summary>
        /// <param name="operation">The simple json patch operation model.</param>
        /// <returns>A valid and strongly typed PatchOperation.</returns>
        public static new PatchOperation<TModel> CreateFromJson(JsonPatchOperation operation)
        {
            ValidatePath(operation);

            return new TestPatchOperation<TModel>(operation.Path, ValidateAndGetValue(operation));
        }

        /// <summary>
        /// Applies the Test patch operation to the target
        /// </summary>
        /// <param name="target">The object to apply the operation to.</param>
        public override void Apply(TModel target)
        {
            this.Apply(
                target,
                (type, parent, current) =>
                {
                    object memberValue = null;
                    if (type.IsList())
                    {
                        var list = (IList)parent;
                        int index;
                        if (int.TryParse(current, out index) &&
                            list.Count > index)
                        {
                            memberValue = list[index];
                        }
                        else
                        {
                            // We can't insert beyond the length of the list.
                            throw new PatchOperationFailedException(PatchResources.IndexOutOfRange(this.Path));
                        }
                    }
                    else if (type.IsDictionary())
                    {
                        var fieldDictionary = ((IDictionary)parent);

                        if (!fieldDictionary.Contains(current))
                        {
                            throw new InvalidPatchFieldNameException(PatchResources.InvalidFieldName(current));
                        }
                        memberValue = fieldDictionary[current];
                    }
                    else
                    {
                        memberValue = type.GetMemberValue(current, parent);
                    }

                    var success = false;
                    if (memberValue != null)
                    {
                        if (memberValue is IList)
                        {
                            // TODO: Implement
                            throw new PatchOperationFailedException(PatchResources.TestNotImplementedForList());
                        }
                        else if (memberValue is IDictionary)
                        {
                            // TODO: Implement
                            throw new PatchOperationFailedException(PatchResources.TestNotImplementedForDictionary());
                        }
                        else if (memberValue.GetType().IsAssignableOrConvertibleFrom(this.Value))
                        {
                            // We convert the objects since we need the values unboxed.
                            var convertedMemberValue = ConvertUtility.ChangeType(memberValue, memberValue.GetType());
                            var convertedValue = ConvertUtility.ChangeType(this.Value, memberValue.GetType());

                            success = convertedMemberValue.Equals(convertedValue);
                        }
                        else
                        {
                            success = memberValue.Equals(this.Value);
                        }
                    }
                    else
                    {
                        success = object.Equals(memberValue, this.Value);
                    }

                    if (!success)
                    {
                        throw new TestPatchOperationFailedException(PatchResources.TestFailed(this.Path, memberValue, this.Value));
                    }
                });
        }
    }
}
