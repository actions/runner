using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITaskTemplateResolver
    {
        Boolean CanResolve(TaskTemplateReference template);

        IList<TaskStep> ResolveTasks(TaskTemplateStep template);
    }
}
