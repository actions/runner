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
    public string EnvironmentName { get; set; }
    public string EnvironmentResourceType { get; set; }
    public Pool Pool { get; set; }

    public Job Parse(Runner.Server.Azure.Devops.Context context, TemplateToken source) {
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
                    Condition = kv.Value.AssertLiteralString("condition");
                break;
                case "strategy":
                    Strategy = new Strategy();
                    foreach(var sv in kv.Value.AssertMapping("strategy")) {
                        if(DeploymentJob) {
                            Action<MappingToken, Strategy.RunOnceStrategy> parseRunOnce = (src, runOnce) => {
                                foreach(var uv in src) {
                                    switch(uv.Key.AssertString("").Value) {
                                        case "preDeploy":
                                        runOnce.PreDeploy = new Strategy.DeploymentHook().Parse(context, uv.Value);
                                        break;
                                        case "deploy":
                                        runOnce.Deploy = new Strategy.DeploymentHook().Parse(context, uv.Value);
                                        break;
                                        case "routeTraffic":
                                        runOnce.RouteTraffic = new Strategy.DeploymentHook().Parse(context, uv.Value);
                                        break;
                                        case "postRouteTraffic":
                                        runOnce.PostRouteTraffic = new Strategy.DeploymentHook().Parse(context, uv.Value);
                                        break;
                                        case "on":
                                        foreach(var kv2 in uv.Value as MappingToken) {
                                            switch(kv2.Key.AssertString("").Value) {
                                            case "failure":
                                                runOnce.OnFailure = new Strategy.DeploymentHook().Parse(context, kv2.Value);
                                                break;
                                            case "success":
                                                runOnce.OnSuccess = new Strategy.DeploymentHook().Parse(context, kv2.Value);
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
                                    Strategy.Canary.Increments = (from inc in (from k in sv.Value as MappingToken where k.Key.AssertString("").Value == "increments" select k.Value).First().AssertSequence("") select inc.AssertNumber("").Value).ToArray();
                                break;
                                case "rolling":
                                    Strategy.Rolling = new Strategy.RollingStrategy();
                                    parseRunOnce(sv.Value as MappingToken, Strategy.Rolling);
                                    var maxParallel = (from k in sv.Value as MappingToken where k.Key.AssertString("").Value == "maxParallel" select k.Value).FirstOrDefault();
                                    if(maxParallel is NumberToken num) {
                                        Strategy.Rolling.MaxParallel = (int)num.Value;
                                    } else if(maxParallel is StringToken percent) {
                                        Strategy.Rolling.MaxParallelPercent = Int32.Parse(percent.Value.Substring(0, percent.Value.IndexOf("%")));
                                    }
                                break;
                            }
                        } else {
                            switch(sv.Key.AssertString("key").Value) {
                                case "parallel":
                                    Strategy.Parallel = (int)sv.Value.AssertNumber("parallel").Value;
                                break;
                                case "matrix":
                                    if(sv.Value is StringToken stringToken) {
                                        Strategy.MatrixExpression = stringToken.Value;
                                    } else {
                                        Strategy.Matrix = sv.Value.AssertMapping("matrix").ToDictionary(mv => mv.Key.AssertString("mk").Value, mv => mv.Value.AssertMapping("matrix").ToDictionary(uv => uv.Key.AssertString("mk").Value, uv => uv.Value.AssertString("mk").Value));
                                    }
                                break;
                                case "maxParallel":
                                    Strategy.MaxParallel = (int)sv.Value.AssertNumber("maxParallel").Value;
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
                    Services = kv.Value.AssertMapping("services").ToDictionary(mv => mv.Key.AssertString("services").Value, mv => new Container().Parse(mv.Value));
                break;
                case "steps":
                    Steps = new List<TaskStep>();
                    foreach(var step2 in kv.Value.AssertSequence("")) {
                        AzureDevops.ParseSteps(context, Steps, step2);
                    }
                break;
                case "templateContext":
                    TemplateContext = kv.Value;
                break;
                case "pool":
                    Pool = new Pool().Parse(context, kv.Value);
                break;
                case "environment":
                    if(kv.Value is StringToken envstkn) {
                        EnvironmentName = envstkn.ToString();
                    } else {
                        foreach(var envm in kv.Value.AssertMapping("environment")) {
                            switch(envm.Key.ToString()) {
                                case "name":
                                    EnvironmentName = envm.Value.ToString();
                                break;
                                case "resourceType":
                                    EnvironmentResourceType = envm.Value.ToString();
                                break;
                            }
                        }
                    }
                break;
            }
        }
        return this;
    }

    public static void ParseJobs(Context context, List<Job> jobs, SequenceToken jobsToken) {
        foreach(var job in jobsToken) {
            if(job is MappingToken mstep && mstep.Count > 0) {
                if((mstep[0].Key as StringToken)?.Value == "template") {
                    var path = (mstep[0].Value as StringToken)?.Value;
                    var file = AzureDevops.ReadTemplate(context, path, mstep.Count == 2 ? mstep[1].Value.AssertMapping("param").ToDictionary(kv => kv.Key.AssertString("").Value, kv => kv.Value) : null, "job-template-root");
                    ParseJobs(context.ChildContext(file, path), jobs, (from e in file where e.Key.AssertString("").Value == "jobs" select e.Value).First().AssertSequence(""));
                } else {
                    jobs.Add(new Job().Parse(context, mstep));
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
        } else {
            if(EnvironmentName != null) {
                if(EnvironmentResourceType != null) {
                    var envm = new DictionaryContextData();
                    envm["name"] = new StringContextData(EnvironmentName);
                    envm["resourceType"] = new StringContextData(EnvironmentResourceType);
                    job["environment"] = envm;
                } else {
                    job["environment"] = new StringContextData(EnvironmentName);
                }
            }
        }
        if(Pool != null) {
            job["pool"] = Pool.ToContextData();
        }
        return job;
    }
}
}
