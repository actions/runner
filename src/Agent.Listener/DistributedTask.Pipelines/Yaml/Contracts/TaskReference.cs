using System;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts
{
    internal sealed class TaskReference
    {
        internal String Name { get; set; }

        internal String Version { get; set; }

        internal TaskReference Clone()
        {
            return new TaskReference
            {
                Name = Name,
                Version = Version,
            };
        }
    }
}
