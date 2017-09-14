using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts
{
    internal class Phase : IPhase
    {
        internal String Condition { get; set; }

        internal String ContinueOnError { get; set; }

        internal IList<String> DependsOn { get; set; }

        internal String EnableAccessToken { get; set; }

        internal String Name { get; set; }

        internal IList<IStep> Steps { get; set; }

        internal IPhaseTarget Target { get; set; }

        internal IList<IVariable> Variables { get; set; }
    }
}
