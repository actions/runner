using System.Collections.Generic;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;

namespace Runner.Server.Azure.Devops {

public class Strategy {
    public int? Parallel { get; set; }
    public Dictionary<string, Dictionary<string, string>> Matrix { get; set; }
    public int? MaxParallel { get; set; }
    public RunOnceStrategy RunOnce { get; internal set; }
    public CanaryStrategy Canary { get; internal set; }
    public RollingStrategy Rolling { get; internal set; }

    public class DeploymentHook {
        public List<TaskStep> Steps { get; set; }
        public Pool Pool { get; set; }

        public DeploymentHook Parse(Context context, TemplateToken src, Dictionary<string, TaskMetaData> tasksByNameAndVersion) {
            foreach(var kv in src.AssertMapping("")) {
                switch(kv.Key.AssertString("").Value) {
                    case "steps":
                        Steps = new List<TaskStep>();
                        foreach(var step2 in kv.Value.AssertSequence("")) {
                            AzureDevops.ParseSteps(context, Steps, step2, tasksByNameAndVersion);
                        }
                    break;
                    case "pool":
                        Pool = new Pool().Parse(context, kv.Value);
                    break;
                }
            }
            return this;
        }

        public DictionaryContextData ToContextData() {
            var hook = new DictionaryContextData();
            var steps = new ArrayContextData();
            foreach(var step in Steps) {
                steps.Add(step.ToContextData());
            }
            hook["steps"] = steps;
            if(Pool != null) {
                hook["pool"] = Pool.ToContextData();
            }
            return hook;
        }
    }

    public class RunOnceStrategy {
        public DeploymentHook PreDeploy { get; set; }
        public DeploymentHook Deploy { get; set; }
        public DeploymentHook RouteTraffic { get; set; }
        public DeploymentHook PostRouteTraffic { get; set; }
        public DeploymentHook OnFailure { get; set; }
        public DeploymentHook OnSuccess { get; set; }
        public DictionaryContextData ToContextData() {
            var runOnceStrategy = new DictionaryContextData();
            if(PreDeploy != null) {
                runOnceStrategy["preDeploy"] = PreDeploy.ToContextData();
            }
            if(Deploy != null) {
                runOnceStrategy["deploy"] = Deploy.ToContextData();
            }
            if(RouteTraffic != null) {
                runOnceStrategy["routeTraffic"] = RouteTraffic.ToContextData();
            }
            if(PostRouteTraffic != null) {
                runOnceStrategy["postRouteTraffic"] = PostRouteTraffic.ToContextData();
            }
            var on = new DictionaryContextData();
            if(OnFailure != null) {
                on["failure"] = OnFailure.ToContextData();
            }
            if(OnSuccess != null) {
                on["success"] = OnSuccess.ToContextData();
            }
            if(OnFailure != null || OnSuccess != null) {
                runOnceStrategy["on"] = on;
            }
            return runOnceStrategy;
        }
    }

    public class CanaryStrategy : RunOnceStrategy {
        public string[] Increments { get; set; }
    }

    public class RollingStrategy : RunOnceStrategy {
        public int MaxParallel { get; set; }
    }



    public DictionaryContextData ToContextData() {
        var strategy = new DictionaryContextData();
        if(Parallel != null) {
            strategy["parallel"] = new NumberContextData(Parallel.Value);
        } else if(Matrix != null) {
            if(MaxParallel != null) {
                strategy["maxParallel"] = new NumberContextData(MaxParallel.Value);
            }
            var matrix = new DictionaryContextData();
            strategy["matrix"] = matrix;
            foreach(var job in Matrix) {
                var vars = new DictionaryContextData();
                foreach(var v in job.Value) {
                    vars[v.Key] = new StringContextData(v.Value);
                }
                matrix[job.Key] = vars;
            }
        } else if(RunOnce != null) {
            strategy["runOnce"] = RunOnce.ToContextData();
        } else if(Rolling != null) {
            strategy["rolling"] = Rolling.ToContextData();
        } else if(Canary != null) {
            strategy["canary"] = Canary.ToContextData();
        }
        return strategy;
    }
}
}