using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GitHub.Services.Common;
using GitHub.Services.WebApi.Patch.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.Services.WebApi.Patch
{
    /// <summary>
    /// PatchOperation represents a single JSON Patch operation.
    /// </summary>
    /// <typeparam name="TModel">The model to validate and apply the patch operation against.</typeparam>
    public abstract class PatchOperation<TModel> : IPatchOperation<TModel>
    {
        /// <summary>
        /// The JSON Patch representation of insertion at the end of a list.
        /// </summary>
        public const string EndOfIndex = "-";

        /// <summary>
        /// The JSON Patch path separator.
        /// </summary>
        public const string PathSeparator = "/";

        /// <summary>
        /// The serializer that handles the object dictionary case.
        /// </summary>
        private static JsonSerializer serializer;

        /// <summary>
        /// The path split into a string IEnumerable.
        /// </summary>
        private IEnumerable<string> evaluatedPath;

        /// <summary>
        /// Static constructor to create the serializer once with the 
        /// ObjectDictionaryConverter which converts JObject to dictionary
        /// when the underlying type of the target is an object.
        /// </summary>
        static PatchOperation()
        {
            serializer = new JsonSerializer();
            serializer.Converters.Add(new ObjectDictionaryConverter());
        }

        /// <summary>
        /// Event fired before applying a patch operation.
        /// </summary>
        public event PatchOperationApplyingEventHandler PatchOperationApplying;

        /// <summary>
        /// Event fired after a patch operation has been applied.
        /// </summary>
        public event PatchOperationAppliedEventHandler PatchOperationApplied;

        /// <summary>
        /// The operation to perform.
        /// </summary>
        public Operation Operation { get; protected set; }

        /// <summary>
        /// The JSON path to apply on the model for this operation.
        /// </summary>
        public string Path { get; protected set; }

        /// <summary>
        /// The path to apply that has been converted to an IEnumerable.
        /// </summary>
        public IEnumerable<string> EvaluatedPath
        {
            get
            {
                if (this.evaluatedPath == null && this.Path != null)
                {
                    this.evaluatedPath = SplitPath(this.Path);
                }

                return this.evaluatedPath;
            }
        }

        /// <summary>
        /// The path to copy/move from, applies only to the Copy/Move operation.
        /// </summary>
        public string From { get; protected set; }

        /// <summary>
        /// The value to set with this patch operation.  Only applies to
        /// Add/Replace/Test.
        /// </summary>
        /// <returns>The strongly (best effort) typed representation of the value.</returns>
        public object Value { get; protected set; }

        /// <summary>
        /// Applies the operation to the target object.
        /// </summary>
        /// <param name="target">The object to have the operation applied to.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public abstract void Apply(TModel target);

        /// <summary>
        /// Creates the strongly typed PatchOperation from the json patch operation provided.
        /// </summary>
        /// <param name="operation">The json patch operation.</param>
        /// <returns>The strongly typed patch operation.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static PatchOperation<TModel> CreateFromJson(JsonPatchOperation operation)
        {
            if (operation != null)
            {
                switch (operation.Operation)
                {
                    case Operation.Add:
                        return AddPatchOperation<TModel>.CreateFromJson(operation);
                    case Operation.Replace:
                        return ReplacePatchOperation<TModel>.CreateFromJson(operation);
                    case Operation.Test:
                        return TestPatchOperation<TModel>.CreateFromJson(operation);
                    case Operation.Remove:
                        return RemovePatchOperation<TModel>.CreateFromJson(operation);
                    default:
                        throw new PatchOperationFailedException(PatchResources.MoveCopyNotImplemented());
                }
            }

            throw new VssPropertyValidationException("Operation", PatchResources.InvalidOperation());
        }

        /// <summary>
        /// Validates the path for the operation.
        /// </summary>
        protected static void ValidatePath(JsonPatchOperation operation)
        {
            // Path cannot be null, but it can be empty.
            if (operation.Path == null)
            {
                throw new VssPropertyValidationException("Path", PatchResources.PathCannotBeNull());
            }

            // If it is not empty and does not start with /, this is an error per RFC.
            if (!operation.Path.StartsWith(PathSeparator) && !string.IsNullOrEmpty(operation.Path))
            {
                throw new VssPropertyValidationException("Path", PatchResources.PathInvalidStartValue());
            }

            // Ending in / is not valid..
            if (operation.Path.EndsWith(PathSeparator))
            {
                throw new VssPropertyValidationException("Path", PatchResources.PathInvalidEndValue());
            }

            // Only add operations allow insert.
            if (operation.Operation != Operation.Add)
            {
                if (operation.Path.EndsWith(EndOfIndex))
                {
                    throw new VssPropertyValidationException("Path", PatchResources.InsertNotSupported(operation.Operation));
                }
            }
        }

        /// <summary>
        /// Validates the type for the operation.
        /// </summary>
        protected static void ValidateType(JsonPatchOperation operation)
        {
            ValidateAndGetType(operation);
        }

        /// <summary>
        /// Validates and returns the type for the operation.
        /// </summary>
        /// <param name="operation"></param>
        /// <returns></returns>
        protected static Type ValidateAndGetType(JsonPatchOperation operation)
        {
            var type = GetType(typeof(TModel), operation.Path);
            if (type == null)
            {
                throw new VssPropertyValidationException("Path", PatchResources.UnableToEvaluatePath(operation.Path));
            }

            return type;
        }

        /// <summary>
        /// Validates the path evaluates to a property on the model, and
        /// returns the strongly typed value for the model.
        /// </summary>
        protected static object ValidateAndGetValue(JsonPatchOperation operation)
        {
            var type = ValidateAndGetType(operation);

            object value;
            if (operation.Value == null)
            {
                value = null;
            }
            else
            {
                value = DeserializeValue(type, operation.Value);
            }

            return value;
        }

        /// <summary>
        /// Gets The type of the field the path maps to.
        /// </summary>
        /// <param name="type">The type of the parent object.</param>
        /// <param name="path">The path to evaluate.</param>
        /// <returns>The type of the field that path maps to.</returns>
        private static Type GetType(Type type, string path)
        {
            return GetType(type, SplitPath(path));
        }

        /// <summary>
        /// Gets The type of the field the path maps to.
        /// </summary>
        /// <param name="type">The type of the parent object.</param>
        /// <param name="path">The path enumeration to evaluate.</param>
        /// <returns>The type of the field that path maps to.</returns>
        private static Type GetType(Type type, IEnumerable<string> path)
        {
            var current = path.First();
            Type currentType = null;

            // The start of the path should always be an empty string after splitting.
            if (string.IsNullOrEmpty(current))
            {
                currentType = type;
            }
            else if (type.IsList())
            {
                currentType = type.GenericTypeArguments[0];
            }
            else if (type.IsDictionary())
            {
                currentType = type.GenericTypeArguments[1];
            }
            else
            {
                currentType = type.GetMemberType(current);
            }

            // Couldn't map the type, return null and let consumer handle.
            if (currentType == null)
            {
                return null;
            }
            // The end of the list, this must be the type we're looking for.
            else if (path.Count() == 1)
            {
                return currentType;
            }
            else
            {
                return GetType(currentType, path.Skip(1));
            }
        }

        /// <summary>
        /// Deserializes the json value.  
        /// </summary>
        /// <param name="type"></param>
        /// <param name="jsonValue">The json formatted value.</param>
        /// <returns>The strongly typed (best effort) value.</returns>
        private static object DeserializeValue(Type type, object jsonValue)
        {
            object value = null;
            if (jsonValue is JToken)
            {
                try
                {
                    value = ((JToken)jsonValue).ToObject(type, serializer);
                }
                catch (JsonException ex)
                {
                    throw new VssPropertyValidationException("Value", PatchResources.InvalidValue(jsonValue, type), ex);
                }
            }
            else
            {
                // Not a JToken, so it must be a primitive type.  Will
                // attempt to convert to the requested type.
                if (type.IsAssignableOrConvertibleFrom(jsonValue))
                {
                    value = ConvertUtility.ChangeType(jsonValue, type);
                }
                else
                {
                    Guid guidValue;
                    if (Guid.TryParse((string)jsonValue, out guidValue))
                    {
                        value = guidValue;
                    }
                    else
                    {
                        throw new VssPropertyValidationException("Value", PatchResources.InvalidValue(jsonValue, type));
                    }
                }
            }

            return value;
        }

        /// <summary>
        /// Converts the string path into the evaluatable path.
        /// </summary>
        private static IEnumerable<string> SplitPath(string path)
        {
            return path.Split(new[] { PathSeparator }, StringSplitOptions.None);
        }

        /// <summary>
        /// Evaluates the path on the target and applies an action to the result.
        /// </summary>
        /// <param name="target">The target object to apply the operation to.</param>
        /// <param name="actionToApply">The action to apply to the result of the evaluation.</param>
        protected void Apply(object target, Action<Type, object, string> actionToApply)
        {
            this.Apply(target, this.EvaluatedPath, actionToApply);
        }

        /// <summary>
        /// Evaluates the path on the target and applies an action to the result.
        /// </summary>
        /// <param name="target">The target object to apply the operation to.</param>
        /// <param name="path">The path to evaluate.</param>
        /// <param name="actionToApply">The action to apply to the result of the evaluation.</param>
        private void Apply(object target, IEnumerable<string> path, Action<Type, object, string> actionToApply)
        {
            var current = path.First();
            var type = target.GetType();

            // We're at the end, time to apply the action.  
            if (path.Count() == 1)
            {
                if (PatchOperationApplying != null)
                {
                    PatchOperationApplying(this, new PatchOperationApplyingEventArgs(this.EvaluatedPath, this.Operation));
                }

                actionToApply(type, target, current);

                if (PatchOperationApplied != null)
                {
                    PatchOperationApplied(this, new PatchOperationAppliedEventArgs(this.EvaluatedPath, this.Operation));
                }
            }
            else
            {
                object newTarget = null;

                // The start of the path should always be an empty string after splitting.
                // We just assign target to new target and move down the path.
                if (string.IsNullOrEmpty(current))
                {
                    newTarget = target;
                }
                // If the next level is a dictionary, we want to get object at the key.
                else if (type.IsDictionary())
                {
                    var dictionary = ((IDictionary)target);
                    if (dictionary.Contains(current))
                    {
                        newTarget = dictionary[current];
                    }
                }
                else if (type.IsList())
                {
                    var list = (IList)target;
                    int index;
                    if (int.TryParse(current, out index) &&
                        list.Count > index)
                    {
                        newTarget = ((IList)target)[index];
                    }
                }
                else
                {
                    newTarget = type.GetMemberValue(current, target);
                }

                if (newTarget == null)
                {
                    // An extra layer of protection, since this should never happen because the earlier call to GetType would have failed.
                    throw new PatchOperationFailedException(PatchResources.TargetCannotBeNull());
                }

                this.Apply(newTarget, path.Skip(1), actionToApply);
            }
        }
    }
}
