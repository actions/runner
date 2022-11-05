using System;
using System.Collections.Generic;
using Runner.Server.Azure.Devops;

namespace Runner.Server
{
    public class LambdaTaskCache : ITaskByNameAndVersionProvider
    {
        public Func<string, TaskMetaData> Resolver { get; set; }

        public TaskMetaData Resolve(string nameAndVersion) {
            return Resolver?.Invoke(nameAndVersion);
        }
    }
}