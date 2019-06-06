using System.Collections.Generic;

namespace GitHub.Services.WebApi.Patch
{
    /// <summary>
    /// The interface for the Patch Document
    /// </summary>
    /// <typeparam name="TModel">The type this patch document applies to.</typeparam>
    public interface IPatchDocument<TModel> : IPatchOperationApplied, IPatchOperationApplying
    {
        /// <summary>
        /// The patch operations.
        /// </summary>
        IEnumerable<IPatchOperation<TModel>> Operations { get; }

        /// <summary>
        /// Applies the operations to the target object.
        /// </summary>
        /// <param name="target">The object to apply the operations to.</param>
        void Apply(TModel target);
    }
}
