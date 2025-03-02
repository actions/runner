using System;
using System.Collections.Generic;
using System.Threading;
using GitHub.DistributedTask.Expressions2;
using Newtonsoft.Json.Linq;

namespace Runner.Server.Services
{
    public class WorkflowContext {
        public CancellationToken CancellationToken {get;set;}
        public CancellationToken? ForceCancellationToken {get;set;}
        public IList<string> FileTable { get; set; }
        public string FileName { get; set; }
        public string WorkflowRunName { get;set; }
        public JObject EventPayload { get; set; }
        public HashSet<string> ReferencedWorkflows { get; } = new HashSet<string>();
        public IDictionary<string, string> FeatureToggles { get; set; }
        public ExpressionFlags Flags { get; internal set; }

        public WorkflowState WorkflowState { get; set; }

        public Azure.Devops.Context AzContext { get; set; }

        public bool HasFeature(string name, bool def = false) {
            return FeatureToggles.TryGetValue(name, out var anchors) ? string.Equals(anchors, "true", StringComparison.OrdinalIgnoreCase) : def;
        }
    }
}