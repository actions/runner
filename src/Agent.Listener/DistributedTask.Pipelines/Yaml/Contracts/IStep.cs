using System;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts
{
    internal interface IStep
    {
        String Name { get; set; }
    }
}
