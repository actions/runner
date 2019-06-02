using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides a contract for resolving tasks from a given store.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITaskStore
    {
        /// <summary>
        /// Resolves a task from the store using the unqiue identifier and version.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task</param>
        /// <param name="version">The version of the task which is desired</param>
        /// <returns>The closest matching task definition if found; otherwise, null</returns>
        TaskDefinition ResolveTask(Guid taskId, String version);

        /// <summary>
        /// Resolves a task from the store using the specified name and version.
        /// </summary>
        /// <param name="name">The name of the task</param>
        /// <param name="version">The version of the task which is desired</param>
        /// <returns>The closest matching task definition if found; otherwise, null</returns>
        TaskDefinition ResolveTask(String name, String version);
    }
}
