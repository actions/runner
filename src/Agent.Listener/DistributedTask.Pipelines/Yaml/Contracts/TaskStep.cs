using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts
{
    internal sealed class TaskStep : ISimpleStep
    {
        public String Name { get; set; }

        internal String Condition { get; set; }

        internal Boolean ContinueOnError { get; set; }

        internal Boolean Enabled { get; set; }

        internal IDictionary<String, String> Environment { get; set; }

        internal IDictionary<String, String> Inputs { get; set; }

        internal TaskReference Reference { get; set; }

        internal Int32 TimeoutInMinutes { get; set; }

        public ISimpleStep Clone()
        {
            return new TaskStep()
            {
                Name = Name,
                Condition = Condition,
                ContinueOnError = ContinueOnError,
                Enabled = Enabled,
                Environment = new Dictionary<String, String>(Environment ?? new Dictionary<String, String>(0, StringComparer.Ordinal)),
                Inputs = new Dictionary<String, String>(Inputs ?? new Dictionary<String, String>(0, StringComparer.OrdinalIgnoreCase)),
                Reference = Reference?.Clone(),
                TimeoutInMinutes = TimeoutInMinutes,
            };
        }
    }
}
