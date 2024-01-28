using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Runner.Server.Azure.Devops
{
    public class Job : IContextDataProvider {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string[] DependsOn { get; set; }
        public string Condition { get; set; }
        public Strategy Strategy { get; set; }
        public string ContinueOnError { get; set; }
        public Container Container { get; set; }
        public string TimeoutInMinutes { get; set; }
        public string CancelTimeoutInMinutes { get; set; }
        public Dictionary<string, VariableValue> Variables { get; set; }
        private Dictionary<string, VariableValue> variablesMetaData;
        public Dictionary<string, Container> Services { get; set; }
        public List<TaskStep> Steps { get; set; }
        public TemplateToken TemplateContext { get; set; }

        public bool DeploymentJob { get; set; }
        public string EnvironmentName { get; set; }
        public string EnvironmentResourceType { get; set; }
        public Pool Pool { get; set; }
        public string[] UsesRepositories { get; set; }
        public string[] UsesPools { get; set; }
        public string WorkspaceClean { get; set; }

        public async Task<Job> Parse(Context context, TemplateToken source, bool skipRootCheck = false) {
            var jobToken = source.AssertMapping("job-root");
            if(!skipRootCheck && (jobToken.Count == 0 || (jobToken[0].Key.ToString() != "job" && jobToken[0].Key.ToString() != "deployment"))) {
                throw new TemplateValidationException(new [] {new TemplateValidationError($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(jobToken)}Expected either job or deployment")});
            }
            var errors = new List<TemplateValidationError>();
            foreach(var kv in jobToken) {
                switch(kv.Key.AssertString("key").Value) {
                    case "job":
                        Name = kv.Value.AssertLiteralString("name");
                    break;
                    case "deployment":
                        Name = kv.Value.AssertLiteralString("name");
                        DeploymentJob = true;
                    break;
                    case "displayName":
                        DisplayName = kv.Value.AssertLiteralString("name");
                    break;
                    case "dependsOn":
                        DependsOn = (from dep in kv.Value.AssertScalarOrSequence("dependsOn") select dep.AssertLiteralString("dep")).ToArray();
                    break;
                    case "condition":
                        Condition = kv.Value.AssertLiteralString("condition");
                    break;
                    case "strategy":
                        Strategy = new Strategy();
                        foreach(var sv in kv.Value.AssertMapping("strategy")) {
                            if(DeploymentJob) {
                                Func<MappingToken, Strategy.RunOnceStrategy, Task> parseRunOnce = async (src, runOnce) => {
                                    foreach(var uv in src) {
                                        switch(uv.Key.AssertString("").Value) {
                                            case "preDeploy":
                                            runOnce.PreDeploy = await new Strategy.DeploymentHook().Parse(context, uv.Value);
                                            break;
                                            case "deploy":
                                            runOnce.Deploy = await new Strategy.DeploymentHook().Parse(context, uv.Value);
                                            break;
                                            case "routeTraffic":
                                            runOnce.RouteTraffic = await new Strategy.DeploymentHook().Parse(context, uv.Value);
                                            break;
                                            case "postRouteTraffic":
                                            runOnce.PostRouteTraffic = await new Strategy.DeploymentHook().Parse(context, uv.Value);
                                            break;
                                            case "on":
                                            foreach(var kv2 in uv.Value as MappingToken) {
                                                switch(kv2.Key.AssertString("").Value) {
                                                case "failure":
                                                    runOnce.OnFailure = await new Strategy.DeploymentHook().Parse(context, kv2.Value);
                                                    break;
                                                case "success":
                                                    runOnce.OnSuccess = await new Strategy.DeploymentHook().Parse(context, kv2.Value);
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
                                        await parseRunOnce(sv.Value as MappingToken, Strategy.RunOnce);
                                    break;
                                    case "canary":
                                        Strategy.Canary = new Strategy.CanaryStrategy();
                                        await parseRunOnce(sv.Value as MappingToken, Strategy.Canary);
                                        var rawIncrements = (from k in sv.Value as MappingToken where k.Key.AssertString("").Value == "increments" select k.Value).FirstOrDefault()?.AssertSequence("canary increments");
                                        if(rawIncrements != null) {
                                            Strategy.Canary.Increments = (from inc in rawIncrements select inc.AssertLiteralString("canary increment")).ToArray();
                                        }
                                    break;
                                    case "rolling":
                                        Strategy.Rolling = new Strategy.RollingStrategy();
                                        await parseRunOnce(sv.Value as MappingToken, Strategy.Rolling);
                                        Strategy.Rolling.MaxParallel = (from k in sv.Value as MappingToken where k.Key.AssertString("").Value == "maxParallel" select k.Value).FirstOrDefault()?.AssertLiteralString("maxParallel");
                                    break;
                                }
                            } else {
                                switch(sv.Key.AssertString("key").Value) {
                                    case "parallel":
                                        Strategy.Parallel = sv.Value.AssertLiteralString("parallel");
                                    break;
                                    case "matrix":
                                        if(sv.Value is LiteralToken matrixExpression) {
                                            Strategy.MatrixExpression = matrixExpression.ToString();
                                        } else {
                                            Strategy.Matrix = sv.Value.AssertMapping("matrix").ToDictionary(mv => mv.Key.AssertString("mk").Value, mv => mv.Value.AssertMapping("matrix").ToDictionary(uv => uv.Key.AssertString("mk").Value, uv => uv.Value.AssertLiteralString("mk")));
                                        }
                                    break;
                                    case "maxParallel":
                                        Strategy.MaxParallel = sv.Value.AssertLiteralString("maxParallel");
                                    break;
                                }
                            }
                        }
                    break;
                    case "continueOnError":
                        ContinueOnError = kv.Value.AssertLiteralString("continueOnError");
                    break;
                    case "container":
                        Container = new Container().Parse(kv.Value);
                    break;
                    case "timeoutInMinutes":
                        TimeoutInMinutes = kv.Value.AssertLiteralString("timeoutInMinutes");
                    break;
                    case "cancelTimeoutInMinutes":
                        CancelTimeoutInMinutes = kv.Value.AssertLiteralString("cancelTimeoutInMinutes");
                    break;
                    case "variables":
                        variablesMetaData = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
                        await AzureDevops.ParseVariables(context, variablesMetaData, kv.Value);
                        Variables = variablesMetaData.Where(metaData => !metaData.Value.IsGroup).ToDictionary(metaData => metaData.Key, metaData => metaData.Value, StringComparer.OrdinalIgnoreCase);
                        variablesMetaData = variablesMetaData.Where(metaData => !metaData.Value.IsGroupMember).ToDictionary(metaData => metaData.Key, metaData => metaData.Value, StringComparer.OrdinalIgnoreCase);
                    break;
                    case "services":
                        Services = kv.Value.AssertMapping("services").ToDictionary(mv => mv.Key.AssertLiteralString("services"), mv => new Container().Parse(mv.Value));
                    break;
                    case "steps":
                        Steps = new List<TaskStep>();
                        await AzureDevops.ParseSteps(context, Steps, kv.Value.AssertSequence(""));
                    break;
                    case "templateContext":
                        TemplateContext = AzureDevops.ConvertAllScalarsToString(kv.Value);
                    break;
                    case "pool":
                        Pool = new Pool().Parse(context, kv.Value);
                    break;
                    case "environment":
                        if(kv.Value is LiteralToken envstkn) {
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
                    case "uses":
                        foreach(var envm in kv.Value.AssertMapping("uses")) {
                            switch(envm.Key.ToString()) {
                                case "repositories":
                                    UsesRepositories = envm.Value.AssertSequence("uses.repositories").Select(r => r.ToString()).ToArray();
                                break;
                                case "pools":
                                    UsesPools = envm.Value.AssertSequence("uses.repositories").Select(r => r.ToString()).ToArray();
                                break;
                            }
                        }
                    break;
                    case "workspace":
                        foreach(var envm in kv.Value.AssertMapping("workspace")) {
                            switch(envm.Key.ToString()) {
                                case "clean":
                                    WorkspaceClean = envm.Value.AssertLiteralString("workspace.clean");
                                break;
                            }
                        }
                    break;
                    default:
                        errors.Add(new TemplateValidationError($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(kv.Key)}Unexpected Key {kv.Key}"));
                    break;
                }
            }
            if(errors.Any()) {
                throw new TemplateValidationException(errors);
            }
            return this;
        }

        public static async Task ParseJobs(Context context, List<Job> jobs, SequenceToken jobsToken) {
            var errors = new List<TemplateValidationError>();
            foreach(var job in jobsToken) {
                try {
                    if(job is MappingToken mstep && mstep.Count > 0) {
                        if((mstep[0].Key as StringToken)?.Value == "template") {
                            var path = (mstep[0].Value as LiteralToken)?.ToString();
                            if(mstep.Count == 2 && (mstep[1].Key as StringToken)?.Value != "parameters") {
                                errors.Add(new TemplateValidationError($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(mstep[1].Key)}Unexpected yaml key {(mstep[1].Key as StringToken)?.Value} expected parameters"));
                                continue;    
                            }
                            if(mstep.Count > 2) {
                                errors.Add(new TemplateValidationError($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(mstep[2].Key)}Unexpected yaml keys {(mstep[2].Key as StringToken)?.Value} after template reference"));
                                continue;
                            }
                            MappingToken file;
                            try {
                                try {
                                    file = await AzureDevops.ReadTemplate(context, path, mstep.Count == 2 ? mstep[1].Value.AssertMapping("param").ToDictionary(kv => kv.Key.AssertString("").Value, kv => kv.Value) : null, "job-template-root");
                                } catch(Exception ex) when (!(ex is TemplateValidationException)) {
                                    errors.Add(new TemplateValidationError($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(mstep[0].Key)}{ex.Message}"));
                                    continue;
                                }
                                await ParseJobs(context.ChildContext(file, path), jobs, (from e in file where e.Key.AssertString("").Value == "jobs" select e.Value).First().AssertSequence(""));
                            } catch(TemplateValidationException ex) {
                                throw new TemplateValidationException(ex.Errors.Prepend(new TemplateValidationError($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(mstep[0].Key)}Found Errors inside Template Reference: {ex.Message}")));
                            }
                        } else {
                            jobs.Add(await new Job().Parse(context, mstep));
                        }
                    }
                } catch(TemplateValidationException ex) {
                    errors.AddRange(ex.Errors);
                } catch(Exception ex) {
                    errors.Add(new TemplateValidationError($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(job)}{ex.Message}"));
                }
            }
            if(errors.Any()) {
                throw new TemplateValidationException(errors);
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
            if(ContinueOnError != null) {
                job["continueOnError"] = new StringContextData(ContinueOnError);
            }
            if(TimeoutInMinutes != null) {
                job["timeoutInMinutes"] = new StringContextData(TimeoutInMinutes);
            }
            if(CancelTimeoutInMinutes != null) {
                job["cancelTimeoutInMinutes"] = new StringContextData(CancelTimeoutInMinutes);
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
            if(variablesMetaData != null) {
                var vars = new ArrayContextData();
                foreach(var v in variablesMetaData) {
                    var varmap = new DictionaryContextData();
                    vars.Add(varmap);
                    if(v.Value.IsGroup) {
                        varmap["group"] = new StringContextData(v.Value.Value);
                    } else {
                        varmap["name"] = new StringContextData(v.Key);
                        varmap["value"] = new StringContextData(v.Value.Value);
                        if(v.Value.IsReadonly) {
                            varmap["readonly"] = new StringContextData("true");
                        }
                    }
                }
                job["variables"] = vars;
            }
            if(!DeploymentJob) {
                // Azure Pipelines seem to ignore missing steps as long as it is only template parameter
                if(Steps != null) {
                    var steps = new ArrayContextData();
                    foreach(var step in Steps) {
                        steps.Add(step.ToContextData());
                    }
                    job["steps"] = steps;
                }
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
            if(UsesRepositories != null || UsesPools != null) {
                var usesRefs = new DictionaryContextData();
                if(UsesRepositories != null) {
                    var usesRepositories = new ArrayContextData();
                    foreach(var repo in UsesRepositories) {
                        usesRepositories.Add(new StringContextData(repo));
                    }
                    usesRefs["repositories"] = usesRepositories;
                }
                if(UsesPools != null) {
                    var usesPools = new ArrayContextData();
                    foreach(var pool in UsesPools) {
                        usesPools.Add(new StringContextData(pool));
                    }
                    usesRefs["pools"] = usesPools;
                }
                job["uses"] = usesRefs;
            }
            if(WorkspaceClean?.Length > 0) {
                var workspace = new DictionaryContextData();
                job["workspace"] = workspace;
                workspace["clean"] = new StringContextData(WorkspaceClean);
            }
            return job;
        }
    }
}
