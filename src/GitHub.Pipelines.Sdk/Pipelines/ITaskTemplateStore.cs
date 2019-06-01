using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides a mechanism for task templates to be resolved at build time.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITaskTemplateStore
    {
        void AddProvider(ITaskTemplateResolver provider);

        IEnumerable<TaskStep> ResolveTasks(TaskTemplateStep step);
    }
}
