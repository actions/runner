using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts
{
    // A phase template cannot reference other phase templates, but
    // steps within can reference templates.
    internal class PhasesTemplate : StepsTemplate
    {
        internal IList<IPhase> Phases { get; set; }
    }
}
