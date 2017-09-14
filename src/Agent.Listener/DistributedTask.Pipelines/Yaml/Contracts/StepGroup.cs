using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts
{
    internal sealed class StepGroup : IStep
    {
        public String Name { get; set; }

        internal IList<ISimpleStep> Steps { get; set; }
    }
}
