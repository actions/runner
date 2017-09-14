using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts
{
    internal class PhasesTemplateReference : StepsTemplateReference, IPhase
    {
        internal IList<PhaseSelector> PhaseSelectors { get; set; }
    }
}
