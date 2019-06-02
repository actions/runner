using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ICounterStore
    {
        /// <summary>
        /// Gets the counters which are allocated for this store.
        /// </summary>
        IReadOnlyDictionary<String, Int32> Counters { get; }

        /// <summary>
        /// Increments the counter with the given prefix. If no such counter exists, a new one will be created with
        /// <paramref name="seed"/> as the initial value.
        /// </summary>
        /// <param name="prefix">The counter prefix</param>
        /// <param name="seed">The initial value for the counter if the counter does not exist</param>
        /// <returns>The incremented value</returns>
        Int32 Increment(IPipelineContext context, String prefix, Int32 seed);
    }
}
