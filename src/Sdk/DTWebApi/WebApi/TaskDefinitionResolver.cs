using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHub.DistributedTask.WebApi
{
    public class TaskDefinitionResolver
    {
        public TaskDefinitionResolver(IList<TaskDefinition> allTasks)
        {
            if (allTasks != null)
            {
                foreach (var taskDefinition in allTasks)
                {
                    Dictionary<TaskVersion, TaskDefinition> versions = null;
                    if (!m_taskMap.TryGetValue(taskDefinition.Id, out versions))
                    {
                        versions = new Dictionary<TaskVersion, TaskDefinition>();
                        m_taskMap.Add(taskDefinition.Id, versions);
                    }
                    // using set instead of add to ignore duplicates
                    versions[taskDefinition.Version] = taskDefinition;
                }
            }
        }

        public Boolean TryResolveTaskReference(
            Guid taskId,
            String versionSpec,
            out TaskDefinition taskDefinition)
        {
            taskDefinition = null;

            // treat missing versionSpec as "*"
            if (String.IsNullOrEmpty(versionSpec))
            {
                versionSpec = "*";
            }

            TaskVersionSpec parsedSpec = null;
            return TaskVersionSpec.TryParse(versionSpec, out parsedSpec)
                && this.TryResolveTaskReference(taskId, parsedSpec, out taskDefinition);
        }

        public Boolean TryResolveTaskReference(
            Guid taskId,
            TaskVersionSpec versionSpec,
            out TaskDefinition taskDefinition)
        {
            taskDefinition = null;

            Dictionary<TaskVersion, TaskDefinition> versions = null;
            if (m_taskMap.TryGetValue(taskId, out versions))
            {
                var matchedVersion = versionSpec.Match(versions.Keys.ToList());
                if (matchedVersion != null)
                {
                    return versions.TryGetValue(matchedVersion, out taskDefinition);
                }
            }

            return false;
        }

        private readonly Dictionary<Guid, Dictionary<TaskVersion, TaskDefinition>> m_taskMap = new Dictionary<Guid, Dictionary<TaskVersion, TaskDefinition>>();
    }
}
