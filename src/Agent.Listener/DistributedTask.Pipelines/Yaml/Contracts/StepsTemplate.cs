using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts
{
    // A step template cannot reference other step templates (enforced during deserialization).
    internal class StepsTemplate
    {
        internal IList<IStep> Steps { get; set; }
    }
}
