using System;
using System.ComponentModel;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITaskResolver
    {
        TaskDefinition Resolve(Guid taskId, String versionSpec);
    }
}
