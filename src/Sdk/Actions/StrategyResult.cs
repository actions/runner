using System.Collections.Generic;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.WebApi;

namespace Sdk.Actions {
    public struct StrategyResult {
        public List<Dictionary<string, TemplateToken>> FlatMatrix { get; set; }
        public List<Dictionary<string, TemplateToken>> IncludeMatrix { get; set; }
        public bool FailFast { get; set; }
        public double? MaxParallel { get; set; }
        public HashSet<string> MatrixKeys { get; set; }
        public TaskResult? Result { get; set; }
    }
}