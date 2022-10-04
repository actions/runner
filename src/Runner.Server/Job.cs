using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;

namespace Runner.Server.Azure.Devops {
public class Job {
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string[] DependsOn { get; set; }
    public string Condition { get; set; }
    public Strategy Strategy { get; set; }
    public bool ContinueOnError { get; set; }
    public Container Container { get; set; }
    public int TimeoutInMinutes { get; set; }
    public int CancelTimeoutInMinutes { get; set; }
    public Dictionary<string, VariableValue> Variables { get; set; }
    public Dictionary<string, Container> Services { get; set; }
    public List<TaskStep> Steps { get; set; }
    public TemplateToken TemplateContext { get; set; }

    public bool DeploymentJob { get; set; }
    public Pool Pool { get; set; }

    public Job Parse(Runner.Server.Azure.Devops.Context context, TemplateToken source, Dictionary<string, TaskMetaData> tasksByNameAndVersion) {
        var jobToken = source.AssertMapping("job-root");
        foreach(var kv in jobToken) {
            switch(kv.Key.AssertString("key").Value) {
                case "job":
                    Name = kv.Value is NullToken ? null : kv.Value.AssertString("name").Value;
                break;
                case "deployment":
                    Name = kv.Value is NullToken ? null : kv.Value.AssertString("name").Value;
                    DeploymentJob = true;
                break;
                case "displayName":
                    DisplayName = kv.Value.AssertString("name").Value;
                break;
                case "dependsOn":
                    DependsOn = (from dep in kv.Value.AssertScalarOrSequence("dependsOn") select dep.AssertString("dep").Value).ToArray();
                break;
                case "condition":
                    Condition = kv.Value.AssertString("condition").Value;
                break;
                case "strategy":
                    Strategy = new Strategy();
                    foreach(var sv in kv.Value.AssertMapping("strategy")) {
                        if(DeploymentJob) {
                            Action<MappingToken, Strategy.RunOnceStrategy> parseRunOnce = (src, runOnce) => {
                                foreach(var kv in src) {
                                    switch(kv.Key.AssertString("").Value) {
                                        case "preDeploy":
                                        runOnce.PreDeploy = new Strategy.DeploymentHook().Parse(context, kv.Value, tasksByNameAndVersion);
                                        break;
                                        case "deploy":
                                        runOnce.Deploy = new Strategy.DeploymentHook().Parse(context, kv.Value, tasksByNameAndVersion);
                                        break;
                                        case "routeTraffic":
                                        runOnce.RouteTraffic = new Strategy.DeploymentHook().Parse(context, kv.Value, tasksByNameAndVersion);
                                        break;
                                        case "postRouteTraffic":
                                        runOnce.PostRouteTraffic = new Strategy.DeploymentHook().Parse(context, kv.Value, tasksByNameAndVersion);
                                        break;
                                        case "on":
                                        foreach(var kv2 in kv.Value as MappingToken) {
                                            switch(kv2.Key.AssertString("").Value) {
                                            case "failure":
                                                runOnce.OnFailure = new Strategy.DeploymentHook().Parse(context, kv2.Value, tasksByNameAndVersion);
                                                break;
                                            case "success":
                                                runOnce.OnSuccess = new Strategy.DeploymentHook().Parse(context, kv2.Value, tasksByNameAndVersion);
                                                break;
                                            }
                                        }
                                        break;
                                    }
                                }
                            };
                            switch(sv.Key.AssertString("key").Value) {
                                case "runOnce":
                                Strategy.RunOnce = new Strategy.RunOnceStrategy();
                                parseRunOnce(sv.Value as MappingToken, Strategy.RunOnce);
                                break;
                                case "canary":
                                Strategy.Canary = new Strategy.CanaryStrategy();
                                parseRunOnce(sv.Value as MappingToken, Strategy.Canary);
                                break;
                                case "rolling":
                                Strategy.Rolling = new Strategy.RollingStrategy();
                                parseRunOnce(sv.Value as MappingToken, Strategy.Rolling);
                                break;
                            }
                        } else {
                            switch(sv.Key.AssertString("key").Value) {
                                case "parallel":
                                Strategy.Parallel = (int)kv.Value.AssertNumber("parallel").Value;
                                break;
                                case "matrix":
                                    Strategy.Matrix = sv.Value.AssertMapping("matrix").ToDictionary(kv => kv.Key.AssertString("mk").Value, kv => kv.Value.AssertMapping("matrix").ToDictionary(kv => kv.Key.AssertString("mk").Value, kv => kv.Value.AssertString("mk").Value));
                                break;
                                case "maxParallel":
                                Strategy.MaxParallel = (int)kv.Value.AssertNumber("maxParallel").Value;
                                break;
                            }
                        }
                    }
                break;
                case "continueOnError":
                    ContinueOnError = kv.Value.AssertBoolean("continueOnError").Value;
                break;
                case "container":
                    Container = new Container().Parse(kv.Value);
                break;
                case "timeoutInMinutes":
                    TimeoutInMinutes = (int)kv.Value.AssertNumber("timeoutInMinutes").Value;
                break;
                case "cancelTimeoutInMinutes":
                    CancelTimeoutInMinutes = (int)kv.Value.AssertNumber("cancelTimeoutInMinutes").Value;
                break;
                case "variables":
                    Variables = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
                    AzureDevops.ParseVariables(context, Variables, kv.Value);
                break;
                case "services":
                    Services = kv.Value.AssertMapping("services").ToDictionary(kv => kv.Key.AssertString("services").Value, kv => new Container().Parse(kv.Value));
                break;
                case "steps":
                    Steps = new List<TaskStep>();
                    foreach(var step2 in kv.Value.AssertSequence("")) {
                        AzureDevops.ParseSteps(context, Steps, step2, tasksByNameAndVersion);
                    }
                break;
                case "templateContext":
                    TemplateContext = kv.Value;
                break;
                case "pool":
                    Pool = new Pool().Parse(context, kv.Value);
                break;
            }
        }
        return this;
    }

    public static void ParseJobs(Context context, List<Job> jobs, SequenceToken jobsToken, Dictionary<string, TaskMetaData> tasksByNameAndVersion) {
        foreach(var job in jobsToken) {
            if(job is MappingToken mstep && mstep.Count > 0) {
                if((mstep[0].Key as StringToken)?.Value == "template") {
                    var path = (mstep[0].Value as StringToken)?.Value;
                    var file = AzureDevops.ReadTemplate(context, path, mstep.Count == 2 ? mstep[1].Value.AssertMapping("param").ToDictionary(kv => kv.Key.AssertString("").Value, kv => kv.Value) : null);
                    ParseJobs(context.ChildContext(file, path), jobs, (from e in file where e.Key.AssertString("").Value == "jobs" select e.Value).First().AssertSequence(""), tasksByNameAndVersion);
                } else {
                    jobs.Add(new Job().Parse(context, mstep, tasksByNameAndVersion));
                }
            }
        }
    }

    public DictionaryContextData ToContextData() {
        var job = new DictionaryContextData();
        job[DeploymentJob ? "deployment" : "job"] = new StringContextData(Name);
        if(DisplayName?.Length > 0) {
            job["displayName"] = new StringContextData(DisplayName);
        }
        if(Condition?.Length > 0) {
            job["condition"] = new StringContextData(Condition);
        }
        if(ContinueOnError) {
            job["continueOnError"] = new BooleanContextData(ContinueOnError);
        }
        if(TimeoutInMinutes > 0) {
            job["timeoutInMinutes"] = new NumberContextData(TimeoutInMinutes);
        }
        if(CancelTimeoutInMinutes > 0) {
            job["cancelTimeoutInMinutes"] = new NumberContextData(CancelTimeoutInMinutes);
        }
        if(Container != null) {
            job["container"] = Container.ToContextData();
        }
        if(Services != null) {
            var services = new DictionaryContextData();
            foreach(var v in Services) {
                services[v.Key] = v.Value.ToContextData();
            }
            job["services"] = services;
        }
        if(TemplateContext != null) {
            job["templateContext"] = TemplateContext.ToContextData();
        }
        if(DependsOn != null) {
            var dependsOn = new ArrayContextData();
            foreach(var dep in DependsOn) {
                dependsOn.Add(new StringContextData(dep));
            }
            job["dependsOn"] = dependsOn;
        }
        if(Strategy != null) {
            job["strategy"] = Strategy.ToContextData();
        }
        if(Variables != null) {
            var vars = new DictionaryContextData();
            foreach(var v in Variables) {
                vars[v.Key] = new StringContextData(v.Value.Value);
            }
            job["variables"] = vars;
        }
        if(!DeploymentJob) {
            var steps = new ArrayContextData();
            foreach(var step in Steps) {
                steps.Add(step.ToContextData());
            }
            job["steps"] = steps;
        }
        if(Pool != null) {
            job["pool"] = Pool.ToContextData();
        }
        return job;
    }
}
}