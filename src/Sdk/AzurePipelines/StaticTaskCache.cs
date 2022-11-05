using System.Collections.Generic;

namespace Runner.Server.Azure.Devops
{
    public class StaticTaskCache : ITaskByNameAndVersionProvider
    {
        public Dictionary<string, TaskMetaData> TasksByNameAndVersion { get; set; }

        public TaskMetaData Resolve(string nameAndVersion) {
            return TasksByNameAndVersion.TryGetValue(nameAndVersion, out var metaData) ? metaData : null;
        }
    }
}