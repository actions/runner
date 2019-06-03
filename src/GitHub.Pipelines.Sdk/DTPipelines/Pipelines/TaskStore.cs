using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GitHub.DistributedTask.Pipelines.Artifacts;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides a mechanism for efficient resolution of task specifications to specific versions of the tasks.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class TaskStore : ITaskStore
    {
        public TaskStore(params TaskDefinition[] tasks)
            : this((IEnumerable<TaskDefinition>)tasks)
        {
        }

        /// <summary>
        /// Constructs a new <c>TaskStore</c> instance with the specified tasks.
        /// </summary>
        /// <param name="tasks">All tasks which should be made available for task resolution</param>
        public TaskStore(
            IEnumerable<TaskDefinition> tasks,
            ITaskResolver resolver = null)
        {
            m_nameLookup = new Dictionary<String, IDictionary<Guid, IList<TaskDefinition>>>(StringComparer.OrdinalIgnoreCase);
            m_tasks = new Dictionary<Guid, IDictionary<String, TaskDefinition>>();
            Resolver = resolver;

            // Filter out legacy tasks with conflicting names.
            //
            // The PublishBuildArtifacts V0 task ID is different from PublishBuildArtifacts V1.
            // Attempts to resolve the PublishBuildArtifacts task by name will result in name conflict.
            // The PublishBuildArtfacts V0 task is not in use anymore. It can simply be removed from
            // the list of tasks, and the naming conflict averted.
            //
            // Additional details: The PublishBuildArtifacts V0 split into two tasks: PublishBuildArtifacts V1
            // and CopyPublishBuildArtifacts V1. The CopyPublishBuildArtifacts V1 task retained the GUID and a
            // new GUID was generated for PublishBuildArtifacts V0. The split happened before task-major-version-locking
            // was implemented. Therefore, no definitions are using the old version.
            tasks = WellKnownTaskDefinitions
                .Concat(tasks?.Where(x => !(x.Id == s_publishBuildArtifacts_v0_ID && x.Version?.Major == 0)) ?? Enumerable.Empty<TaskDefinition>())
                .ToList();

            // Build a lookup of all task versions for a given task identifier
            foreach (var task in tasks)
            {
                AddVersion(task);
            }

            // Filter the tasks to the latest within each major version so we can provide a lookup by name
            var latestTasksByMajorVersion = tasks.GroupBy(x => new { x.Id, x.Version.Major }).Select(x => x.OrderByDescending(y => y.Version).First());
            foreach (var task in latestTasksByMajorVersion)
            {
                // The name should never be null in production environments but just in case don't provide by-name lookup
                // for tasks which don't provide one.
                if (!String.IsNullOrEmpty(task.Name))
                {
                    // Add the name lookup.
                    IDictionary<Guid, IList<TaskDefinition>> tasksByIdLookup;
                    if (!m_nameLookup.TryGetValue(task.Name, out tasksByIdLookup))
                    {
                        tasksByIdLookup = new Dictionary<Guid, IList<TaskDefinition>>();
                        m_nameLookup.Add(task.Name, tasksByIdLookup);
                    }

                    IList<TaskDefinition> tasksById;
                    if (!tasksByIdLookup.TryGetValue(task.Id, out tasksById))
                    {
                        tasksById = new List<TaskDefinition>();
                        tasksByIdLookup.Add(task.Id, tasksById);
                    }

                    tasksById.Add(task);

                    if (!String.IsNullOrEmpty(task.ContributionIdentifier))
                    {
                        // Add the contribution-qualified-name lookup.
                        var qualifiedName = $"{task.ContributionIdentifier}.{task.Name}";
                        if (!m_nameLookup.TryGetValue(qualifiedName, out tasksByIdLookup))
                        {
                            tasksByIdLookup = new Dictionary<Guid, IList<TaskDefinition>>();
                            m_nameLookup.Add(qualifiedName, tasksByIdLookup);
                        }

                        if (!tasksByIdLookup.TryGetValue(task.Id, out tasksById))
                        {
                            tasksById = new List<TaskDefinition>();
                            tasksByIdLookup.Add(task.Id, tasksById);
                        }

                        tasksById.Add(task);
                    }
                }
            }
        }

        public ITaskResolver Resolver
        {
            get;
        }

        /// <summary>
        /// Resolves a task from the store using the unqiue identifier and version.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task</param>
        /// <param name="version">The version of the task which is desired</param>
        /// <returns>The closest matching task definition if found; otherwise, null</returns>
        public TaskDefinition ResolveTask(
            Guid taskId,
            String versionSpec)
        {
            TaskDefinition task = null;

            // Treat missing version as "*"
            if (String.IsNullOrEmpty(versionSpec))
            {
                versionSpec = "*";
            }

            if (m_tasks.TryGetValue(taskId, out IDictionary<String, TaskDefinition> tasks))
            {
                var parsedSpec = TaskVersionSpec.Parse(versionSpec);
                task = parsedSpec.Match(tasks.Values);
            }

            // Read-thru on miss
            if (task == null && Resolver != null)
            {
                task = Resolver.Resolve(taskId, versionSpec);
                if (task != null)
                {
                    AddVersion(task);
                }
            }

            return task;
        }

        /// <summary>
        /// Resolves a task from the store using the specified name and version.
        /// </summary>
        /// <param name="name">The name of the task</param>
        /// <param name="version">The version of the task which is desired</param>
        /// <returns>The closest matching task definition if found; otherwise, null</returns>
        public TaskDefinition ResolveTask(
            String name,
            String versionSpec)
        {
            Guid taskIdentifier;
            if (!Guid.TryParse(name, out taskIdentifier))
            {
                IDictionary<Guid, IList<TaskDefinition>> nameLookup;
                if (!m_nameLookup.TryGetValue(name, out nameLookup))
                {
                    return null;
                }

                if (nameLookup.Count == 1)
                {
                    // Exactly one task ID was resolved.
                    taskIdentifier = nameLookup.Keys.Single();
                }
                else
                {
                    // More than one task ID was resolved.
                    // Prefer in-the-box tasks over extension tasks.
                    var inTheBoxTaskIdentifiers =
                        nameLookup
                        .Where(pair => pair.Value.All(taskDefinition => String.IsNullOrEmpty(taskDefinition.ContributionIdentifier)))
                        .Select(pair => pair.Key)
                        .ToList();
                    if (inTheBoxTaskIdentifiers.Count == 1)
                    {
                        taskIdentifier = inTheBoxTaskIdentifiers[0];
                    }
                    else
                    {
                        // Otherwise, ambiguous.
                        throw new AmbiguousTaskSpecificationException(PipelineStrings.AmbiguousTaskSpecification(name, String.Join(", ", nameLookup.Keys)));
                    }
                }
            }

            return ResolveTask(taskIdentifier, versionSpec);
        }

        private void AddVersion(TaskDefinition task)
        {
            IDictionary<String, TaskDefinition> tasksByVersion;
            if (!m_tasks.TryGetValue(task.Id, out tasksByVersion))
            {
                tasksByVersion = new Dictionary<String, TaskDefinition>(StringComparer.OrdinalIgnoreCase);
                m_tasks.Add(task.Id, tasksByVersion);
            }

            tasksByVersion[task.Version] = task;
        }

        private IDictionary<Guid, IDictionary<String, TaskDefinition>> m_tasks;
        private IDictionary<String, IDictionary<Guid, IList<TaskDefinition>>> m_nameLookup;

        private static readonly Guid s_publishBuildArtifacts_v0_ID = new Guid("1d341bb0-2106-458c-8422-d00bcea6512a");

        private static readonly TaskDefinition[] WellKnownTaskDefinitions = new[]
        {
            PipelineConstants.CheckoutTask,
            PipelineArtifactConstants.DownloadTask,
        };
    }
}
