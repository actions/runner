using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts
{
    internal sealed class VariablesTemplateReference : IVariable
    {
        internal String Name { get; set; }

        internal IDictionary<String, Object> Parameters { get; set; }
    }
}
