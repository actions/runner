using System;
using System.Collections.Generic;
using GitHub.Actions.RunService.WebApi;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Worker.Container;
using Newtonsoft.Json.Linq;

namespace GitHub.Runner.Worker
{
    public sealed class GlobalContext
    {
        public ContainerInfo Container { get; set; }
        public List<ServiceEndpoint> Endpoints { get; set; }
        public IDictionary<String, String> EnvironmentVariables { get; set; }
        public PlanFeatures Features { get; set; }
        public IList<String> FileTable { get; set; }
        public IDictionary<String, IDictionary<String, String>> JobDefaults { get; set; }
        public List<ActionsStepTelemetry> StepsTelemetry { get; set; }
        public List<StepResult> StepsResult { get; set; }
        public List<JobTelemetry> JobTelemetry { get; set; }
        public TaskOrchestrationPlanReference Plan { get; set; }
        public List<string> PrependPath { get; set; }
        public List<ContainerInfo> ServiceContainers { get; set; }
        public StepsContext StepsContext { get; set; }
        public Variables Variables { get; set; }
        public bool WriteDebug { get; set; }
        public Guid JobId {get; set; }
        public JObject ContainerHookState { get; set; }
    }
}
