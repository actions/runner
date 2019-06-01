using System;
using System.ComponentModel;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ICounterResolver
    {
        Int32 Increment(IPipelineContext context, String prefix, Int32 seed);
    }
}