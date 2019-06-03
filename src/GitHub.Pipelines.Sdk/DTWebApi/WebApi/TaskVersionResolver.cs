using System;
using System.Collections.Generic;

namespace GitHub.DistributedTask.WebApi
{
    public sealed class TaskVersionResolver
    {
        public TaskVersionResolver(IDictionary<Guid, IList<TaskVersion>> taskVersions)
            : this(null, taskVersions)
        { 
        }

        public TaskVersionResolver(
            IDictionary<Guid, IDictionary<String, TaskDefinition>> taskDefinitions,
            IDictionary<Guid, IList<TaskVersion>> taskVersions)
        {
            m_taskDefinitions = taskDefinitions ?? new Dictionary<Guid, IDictionary<String, TaskDefinition>>();
            m_taskVersions = taskVersions ?? new Dictionary<Guid, IList<TaskVersion>>();
        }

        public TaskVersion ResolveVersion(
            Guid taskId,
            String version)
        {
            IList<TaskVersion> allVersions;
            if (!m_taskVersions.TryGetValue(taskId, out allVersions))
            {
                throw new NotSupportedException(taskId.ToString("D"));
            }

            var versionSpec = TaskVersionSpec.Parse(version);
            return versionSpec.Match(allVersions);
        }

        public Boolean TryResolveVersion(
            Guid taskId,
            String version,
            out TaskVersion taskVersion)
        {
            taskVersion = null;

            // treat missing version as "*"
            if (String.IsNullOrEmpty(version))
            {
                version = "*";
            }

            IList<TaskVersion> allVersions;
            if (m_taskVersions.TryGetValue(taskId, out allVersions))
            {
                var versionSpec = TaskVersionSpec.Parse(version);
                taskVersion = versionSpec.Match(allVersions);
            }

            return taskVersion != null;
        }

        private IDictionary<Guid, IList<TaskVersion>> m_taskVersions;
        private IDictionary<Guid, IDictionary<String, TaskDefinition>> m_taskDefinitions;
    }
}
