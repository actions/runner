using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts
{
    // A process template cannot reference other process templates, but
    // phases/steps within can reference templates.
    internal sealed class ProcessTemplate : PhasesTemplate
    {
        internal IList<ProcessResource> Resources { get; set; }
    }
}
