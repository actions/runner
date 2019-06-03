using System;
using System.Collections.Generic;
using System.ComponentModel;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class StrategyResult
    {
        public StrategyResult()
        {
            FailFast = true;
        }

        public Boolean FailFast { get; set; }

        public int MaxParallel { get; set; }

        public IList<StrategyConfiguration> Configurations { get; } = new List<StrategyConfiguration>();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class StrategyConfiguration
    {
        public String DisplayName { get; set; }

        public String Name { get; set; }

        public IDictionary<String, PipelineContextData> ContextData { get; } = new Dictionary<String, PipelineContextData>(StringComparer.Ordinal);
    }
}
