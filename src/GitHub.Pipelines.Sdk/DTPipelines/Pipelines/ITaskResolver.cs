using System;
using System.ComponentModel;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITaskResolver
    {
        TaskDefinition Resolve(Guid taskId, String versionSpec);
    }
}
