using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts
{
    internal class PhaseExecution
    {
        internal String ContinueOnError { get; set; }

        internal IDictionary<String, IDictionary<String, String>> Matrix { get; set; }

        internal String MaxConcurrency { get; set; }

        internal String TimeoutInMinutes { get; set; }
    }
}
