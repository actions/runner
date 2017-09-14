using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts
{
    internal sealed class VariablesTemplate
    {
        internal IList<IVariable> Variables { get; set; }
    }
}
