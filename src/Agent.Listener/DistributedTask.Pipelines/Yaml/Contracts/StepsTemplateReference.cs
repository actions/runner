using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts
{
    internal class StepsTemplateReference : IStep
    {
        public String Name { get; set; }

        internal IDictionary<String, Object> Parameters { get; set; }

        internal IDictionary<String, IList<ISimpleStep>> StepOverrides { get; set; }
    }
}
