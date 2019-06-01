using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITaskTemplateResolver
    {
        Boolean CanResolve(TaskTemplateReference template);

        IList<TaskStep> ResolveTasks(TaskTemplateStep template);
    }
}
