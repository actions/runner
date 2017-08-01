using System;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts
{
    internal sealed class Variable : IVariable
    {
        internal String Name { get; set; }

        internal String Value { get; set; }
    }
}
