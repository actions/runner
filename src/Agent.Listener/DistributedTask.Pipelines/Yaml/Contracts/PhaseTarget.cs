using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts
{
    internal sealed class PhaseTarget
    {
        internal IList<String> Demands { get; set; }

        internal String HealthOption { get; set; }

        internal String Name { get; set; }

        internal String Percentage { get; set; }

        internal IList<String> Tags { get; set; }

        internal String Type { get; set; }
    }
}
