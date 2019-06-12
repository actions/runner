using System.Collections.Generic;

namespace GitHub.Services.WebApi.Patch
{
    /// <summary>
    /// The interface for the Patch Operation.
    /// </summary>
    /// <typeparam name="TModel">The type this patch document applies to.</typeparam>
    public interface IPatchOperation<TModel> : IPatchOperationApplied, IPatchOperationApplying
    {
        /// <summary>
        /// The operation to perform.
        /// </summary>
        Operation Operation { get; }

        /// <summary>
        /// The JSON path to apply on the model for this operation.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// The JSON path split into segments 
        /// </summary>
        IEnumerable<string> EvaluatedPath { get; }

        /// <summary>
        /// The path to copy/move from, applies only to the Copy/Move operation.
        /// </summary>
        string From { get;  }

        /// <summary>
        /// The value to set with this patch operation.  Only applies to
        /// Add/Replace/Test.
        /// </summary>
        /// <returns>The strongly (best effort) typed representation of the value.</returns>
        object Value { get; }

        /// <summary>
        /// Applies the operation to the target object.
        /// </summary>
        /// <param name="target">The object to have the operation applied to.</param>
        void Apply(TModel target);
    }
}
