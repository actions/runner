using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts
{
    internal sealed class Process : Phase
    {
        internal IList<IPhase> Phases { get; set; }

        internal IList<ProcessResource> Resources { get; set; }

        internal ProcessTemplateReference Template { get; set; }
    }
}
