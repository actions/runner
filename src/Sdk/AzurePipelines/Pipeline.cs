using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.Expressions2.Sdk.Functions;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runner.Server.Azure.Devops
{

    public class Pipeline : IContextDataProvider {
        public string Name { get; set; }
        public List<Stage> Stages { get; set; }
        public Dictionary<string, VariableValue> Variables { get; set; }
        private Dictionary<string, VariableValue> variablesMetaData;
        public Dictionary<string, Container> ContainerResources { get; set; }
        public Dictionary<string, TemplateToken> OtherResources { get; set; }
        public TemplateToken Trigger { get; set; }
        public TemplateToken Pr { get; set; }
        public TemplateToken Schedules { get; set; }
        public Pool Pool { get; set; }
        public bool? AppendCommitMessageToRunName { get; set; }
        public String LockBehavior { get; set; }

        public async Task<Pipeline> Parse(Context context, TemplateToken source) {
            var pipelineRootToken = source.AssertMapping("pipeline-root");
            Pipeline parent = null;
            MappingToken unparsedTokens = new MappingToken(null, null, null);
            var defers = new List<Func<Task>>();
            foreach(var kv in pipelineRootToken) {
                switch(kv.Key.AssertString("key").Value) {
                    case "name":
                        Name = kv.Value.AssertLiteralString("name");
                    break;
                    case "parameters":
                        break;
                    case "variables":
                        variablesMetaData = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
                        await AzureDevops.ParseVariables(context, variablesMetaData, kv.Value);
                        Variables = variablesMetaData.Where(metaData => !metaData.Value.IsGroup).ToDictionary(metaData => metaData.Key, metaData => metaData.Value, StringComparer.OrdinalIgnoreCase);
                        variablesMetaData = variablesMetaData.Where(metaData => !metaData.Value.IsGroupMember).ToDictionary(metaData => metaData.Key, metaData => metaData.Value, StringComparer.OrdinalIgnoreCase);
                    break;
                    case "extends":
                        var ext = kv.Value.AssertMapping("extends");
                        string template = null;
                        Dictionary<string, TemplateToken> parameters = null;
                        if(ext.Count == 2 && (ext[0].Key as StringToken)?.Value != "template") {
                            throw new TemplateValidationException(new [] {new TemplateValidationError($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(ext[0].Key)}Unexpected yaml key {(ext[0].Key as StringToken)?.Value} expected template")});
                        }
                        if(ext.Count == 2 && (ext[1].Key as StringToken)?.Value != "parameters") {
                            throw new TemplateValidationException(new [] {new TemplateValidationError($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(ext[1].Key)}Unexpected yaml key {(ext[1].Key as StringToken)?.Value} expected parameters")});
                        }
                        if(ext.Count > 2) {
                            throw new TemplateValidationException(new [] {new TemplateValidationError($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(ext[2].Key)}Unexpected yaml keys {(ext[2].Key as StringToken)?.Value} after template reference")});
                        }
                        foreach(var ev in ext) {
                            switch(ev.Key.AssertString("").Value) {
                                case "template":
                                    template = ev.Value.AssertLiteralString("");
                                break;
                                case "parameters":
                                    parameters = ev.Value.AssertMapping("param").ToDictionary(pv => pv.Key.AssertString("").Value, pv => pv.Value);
                                break;
                            }
                        }
                        try {
                            var templ = await AzureDevops.ReadTemplate(context, template, parameters, "extend-template-root");
                            parent = await new Pipeline().Parse(context.ChildContext(templ, template), templ);
                        } catch(TemplateValidationException ex) {
                            throw new TemplateValidationException(ex.Errors.Prepend(new TemplateValidationError($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(ext)}Found Errors inside Template Reference: {ex.Message}")));
                        }
                    break;
                    case "stages":
                        Stages = new List<Stage>();
                        await Stage.ParseStages(context, Stages, kv.Value.AssertSequence("stages"));
                    break;
                    case "steps":
                        unparsedTokens.Add(kv);
                        defers.Add(async () => {
                            var implicitJob = await new Job().Parse(context, unparsedTokens, skipRootCheck: true);
                            implicitJob.Name = null;
                            Stages = new List<Stage>{ new Stage {
                                Jobs = new List<Job>{ implicitJob }
                            } };
                        });
                    break;
                    case "jobs":
                        unparsedTokens.Add(kv);
                        defers.Add(async () => {
                            var implicitStage = await new Stage().Parse(context, unparsedTokens, skipRootCheck: true);
                            implicitStage.Name = null;
                            Stages = new List<Stage>{ implicitStage };
                        });
                    break;
                    case "resources":
                        foreach(var resource in kv.Value.AssertMapping("resources")) {
                            switch(resource.Key.AssertString("").Value) {
                                case "containers":
                                    ContainerResources = new Dictionary<string, Container>(StringComparer.OrdinalIgnoreCase);
                                    foreach(var rawcontainer in resource.Value.AssertSequence("cres")) {
                                        var container = rawcontainer.AssertMapping("");
                                        ContainerResources[container[0].Value.AssertLiteralString("")] = new Container().Parse(container);
                                    }
                                break;
                                default:
                                    if(OtherResources == null) {
                                        OtherResources = new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase);
                                    }
                                    OtherResources[resource.Key.ToString()] = resource.Value;
                                break;
                            } 
                        }
                    break;
                    case "pool":
                        Pool = new Pool().Parse(context, kv.Value);
                    break;
                    case "appendCommitMessageToRunName":
                        AppendCommitMessageToRunName = kv.Value.AssertAzurePipelinesBoolean("appendCommitMessageToRunName have to be of type bool");
                    break;
                    case "lockBehavior":
                        LockBehavior = kv.Value.AssertLiteralString("lockBehavior have to be of type string");
                    break;
                    case "trigger":
                        Trigger = kv.Value;
                    break;
                    case "pr":
                        Pr = kv.Value;
                    break;
                    case "schedules":
                        Schedules = kv.Value;
                    break;
                    default:
                        unparsedTokens.Add(kv);
                    break;
                }
            }
            foreach(var defer in defers) {
                await defer();
            }
            if(parent != null) {
                Stages = parent.Stages;
                if(parent.ContainerResources != null) {
                    if(ContainerResources == null) {
                        ContainerResources = new Dictionary<string, Container>(StringComparer.OrdinalIgnoreCase);
                    }
                    foreach(var cr in parent.ContainerResources) {
                        ContainerResources[cr.Key] = cr.Value;
                    }
                }
                if(parent?.OtherResources != null) {
                    if(OtherResources == null) {
                        OtherResources = new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase);
                    }
                    foreach(var ores in parent.OtherResources) {
                        if(OtherResources.TryGetValue(ores.Key, out var pres)) {
                            var apres = pres.AssertSequence($"resources.{ores.Key} array");
                            foreach(var oresi in ores.Value.AssertSequence($"resources.{ores.Key} array")) {
                                apres.Add(oresi);
                            }
                        } else {
                            OtherResources[ores.Key] = ores.Value;
                        }
                    }
                }
                if(parent.Variables != null) {
                    if (Variables == null) {
                        Variables = parent.Variables;
                        variablesMetaData = parent.variablesMetaData;
                    } else {
                        // emulate the error message provided by Azure Pipelines
                        throw new NotSupportedException("__built-in-schema.yml (Line: 40, Col:11): 'variables' is already defined");
                    }
                }
            }
            return this;
        }

        public DictionaryContextData ToContextData() {
            var pipeline = new DictionaryContextData();
            if(!string.IsNullOrEmpty(Name)) {
                pipeline["name"] = new StringContextData(Name);
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
                pipeline["variables"] = vars;
            }
            if(AppendCommitMessageToRunName != null) {
                pipeline["appendCommitMessageToRunName"] = new StringContextData(AppendCommitMessageToRunName.Value.ToString());
            }
            if(Trigger != null) {
                pipeline["trigger"] = Trigger.ToContextData();
            }
            if(Pr != null) {
                pipeline["pr"] = Pr.ToContextData();
            }
            if(Schedules != null) {
                pipeline["schedules"] = Schedules.ToContextData();
            }
            if(ContainerResources != null || OtherResources != null) {
                var resources = new DictionaryContextData();
                pipeline["resources"] = resources;
                if(ContainerResources != null) {
                    var containers = new ArrayContextData();
                    resources["containers"] = containers;
                    foreach(var cr in ContainerResources) {
                        containers.Add(cr.Value.ToContextData(cr.Key));
                    }
                }
                if(OtherResources != null) {
                    foreach(var ores in OtherResources) {
                        resources[ores.Key] = ores.Value.ToContextData();
                    }
                }
            }
            if(Stages != null) {
                var stageList = new ArrayContextData();
                foreach(var stage in Stages) {
                    stageList.Add(stage.ToContextData());
                }
                pipeline["stages"] = stageList;
            }
            if(Pool != null) {
                pipeline["pool"] = Pool.ToContextData();
            }
            if(LockBehavior != null) {
                pipeline["lockBehavior"] = new StringContextData(LockBehavior);
            }
            return pipeline;
        }

        private class JobItem {
            public string Name { get; set; }
            public HashSet<string> DependsOn { get; set; }
            public string Stage { get; set; }
            public Dictionary<string, JobItem> Dependencies { get; set; }
        }

        public void CheckPipelineForRuntimeFailure() {
            var errors = new TemplateValidationErrors();
            try {
                // Fill empty Names
                InitializeNames();
                // Check Dependency Chain
                CheckDependencyChain();
            } catch(Exception ex) {
                errors.Add(ex.Message);
            }
            CheckRuntimeExpressionSyntax(errors);
            errors.Check();
        }

        private void InitializeNames() {
            var stagenamebuilder = new ReferenceNameBuilder();
            {
                List<string> errors = new List<string>();
                foreach (var stage in Stages) {
                    if(!string.IsNullOrEmpty(stage.Name)) {
                        // Validate StageName
                        if(!stagenamebuilder.TryAddKnownName(stage.Name, out var jnerror)) {
                            errors.Add(jnerror);
                        }
                    }
                }
                if(errors.Count > 0) {
                    var b = new StringBuilder();
                    int i = 0;
                    foreach (var error in errors) {
                        if(i++ != 0) {
                            b.Append(". ");
                        }
                        b.Append(error);
                    }
                    throw new Exception(b.ToString());
                }
            }
            for(int s = 0; s < Stages.Count; s++) {
                var stage = Stages[s];
                // If DependsOn is omitted for a stage, depend on the previous one
                if(stage.DependsOn == null && s > 0) {
                    stage.DependsOn = new [] { Stages[s - 1].Name };
                }
                if(string.IsNullOrEmpty(stage.Name)) {
                    stagenamebuilder.AppendSegment("Stage");
                    stage.Name = stagenamebuilder.Build();
                }
                List<string> errors = new List<string>();
                var jobnamebuilder = new ReferenceNameBuilder();
                if(stage.Jobs != null) {
                    foreach (var job in stage.Jobs) {
                        if(!string.IsNullOrEmpty(job.Name)) {
                            // Validate Jobname
                            if(!jobnamebuilder.TryAddKnownName(job.Name, out var jnerror)) {
                                errors.Add(jnerror);
                            }
                        }
                    }
                }
                if(errors.Count > 0) {
                    var b = new StringBuilder();
                    int i = 0;
                    foreach (var error in errors) {
                        if(i++ != 0) {
                            b.Append(". ");
                        }
                        b.Append(error);
                    }
                    throw new Exception(b.ToString());
                }
                if(stage.Jobs != null) {
                    foreach (var job in stage.Jobs) {
                        if(string.IsNullOrEmpty(job.Name)) {
                            jobnamebuilder.AppendSegment("Job");
                            job.Name = jobnamebuilder.Build();
                        }
                        if(job.Pool == null) {
                            job.Pool = stage.Pool ?? Pool;
                        }
                        var jobname = job.Name;
                        var jobitem = new JobItem() { Name = jobname, Stage = stage.Name };
                        //dependentjobgroup.Add(jobitem);

                        var needs = job.DependsOn;
                        List<string> neededJobs = new List<string>();
                        if (needs != null) {
                            neededJobs.AddRange(needs);
                        }
                        jobitem.DependsOn = neededJobs.ToHashSet(StringComparer.OrdinalIgnoreCase);
                    }
                }
            }
        }

        private void CheckDependencyChain() {
            var dependentjobgroup = Stages.SelectMany(s => s.Jobs?.Select(j => new JobItem { Name = j.Name, DependsOn = (j.DependsOn ?? Array.Empty<string>()).ToHashSet(StringComparer.OrdinalIgnoreCase), Stage = s.Name }) ?? Array.Empty<JobItem>()).ToList();
            dependentjobgroup.ForEach(ji => {
                if(ji.DependsOn?.Any() == true) {
                    Func<JobItem, ISet<string>, Dictionary<string, JobItem>> pred = null;
                    pred = (cur, cyclic) => {
                        var ret = new Dictionary<string, JobItem>(StringComparer.OrdinalIgnoreCase);
                        if(cur.DependsOn?.Any() == true) {
                            // To preserve case of direct dependencies as written in yaml
                            foreach(var need in cur.DependsOn) {
                                ret[need] = null;
                            }
                            var pcyclic = cyclic.Append(cur.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                            ISet<string> missingDeps = cur.DependsOn.ToHashSet(StringComparer.OrdinalIgnoreCase);
                            dependentjobgroup.ForEach(d => {
                                if(!string.Equals(cur.Stage, d.Stage, StringComparison.OrdinalIgnoreCase)) {
                                    return;
                                }
                                if(cur.DependsOn.Contains(d.Name)) {
                                    if(pcyclic.Contains(d.Name)) {
                                        throw new Exception($"{cur.Name}: Cyclic dependency to {d.Name} detected");
                                    }
                                    ret[d.Name] = d;
                                    if(d.Dependencies == null) {
                                        d.Dependencies = pred?.Invoke(d, pcyclic);
                                        foreach (var k in d.Dependencies) {
                                            ret[k.Key] = k.Value;
                                        }
                                    } else {
                                        foreach (var k in d.Dependencies) {
                                            if(pcyclic.Contains(k.Key)) {
                                                throw new Exception($"{cur.Name}: Cyclic dependency to {k.Key} detected");
                                            }
                                            ret[k.Key] = k.Value;
                                        }
                                    }
                                    missingDeps.Remove(d.Name);
                                }
                            });
                            if(missingDeps.Any()) {
                                throw new Exception($"{cur.Name}: One or more missing dependencies detected: {string.Join(", ", missingDeps)}");
                            }
                        }
                        return ret;
                    };
                    if(ji.Dependencies == null)
                        ji.Dependencies = pred(ji, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                }
            });
            var stagesByName = new Dictionary<string, Azure.Devops.Stage>(StringComparer.OrdinalIgnoreCase);
            Stages.ForEach(ji => {
                stagesByName[ji.Name] = ji;
                if(ji.DependsOn?.Any() == true) {
                    Func<Azure.Devops.Stage, ISet<string>, Dictionary<string, Azure.Devops.Stage>> pred = null;
                    pred = (cur, cyclic) => {
                        var ret = new Dictionary<string, Azure.Devops.Stage>(StringComparer.OrdinalIgnoreCase);
                        if(cur.DependsOn?.Any() == true) {
                            // To preserve case of direct dependencies as written in yaml
                            foreach(var need in cur.DependsOn) {
                                ret[need] = null;
                            }
                            var pcyclic = cyclic.Append(cur.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                            ISet<string> missingDeps = cur.DependsOn.ToHashSet(StringComparer.OrdinalIgnoreCase);
                            ISet<string> allDeps = cur.DependsOn.ToHashSet(StringComparer.OrdinalIgnoreCase);
                            Stages.ForEach(d => {
                                if(allDeps.Contains(d.Name)) {
                                    if(pcyclic.Contains(d.Name)) {
                                        throw new Exception($"{cur.Name}: Cyclic dependency to {d.Name} detected");
                                    }
                                    ret[d.Name] = d;
                                    if(d.Dependencies == null) {
                                        d.Dependencies = pred?.Invoke(d, pcyclic);
                                        foreach (var k in d.Dependencies) {
                                            ret[k.Key] = k.Value;
                                        }
                                    } else {
                                        foreach (var k in d.Dependencies) {
                                            if(pcyclic.Contains(k.Key)) {
                                                throw new Exception($"{cur.Name}: Cyclic dependency to {k.Key} detected");
                                            }
                                            ret[k.Key] = k.Value;
                                        }
                                    }
                                    missingDeps.Remove(d.Name);
                                }
                            });
                            if(missingDeps.Any()) {
                                throw new Exception($"{cur.Name}: One or more missing dependencies detected: {string.Join(", ", missingDeps)}");
                            }
                        }
                        return ret;
                    };
                    if(ji.Dependencies == null)
                        ji.Dependencies = pred(ji, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                }
            });
        }

        private enum Level {
            Stage,
            Job,
            Step
        }

        private void CheckRuntimeExpressionSyntax(TemplateValidationErrors errors)
        {
            CheckVariableExpressions(errors, Variables);
            if(Stages != null) {
                foreach(var stage in Stages) {
                    CheckVariableExpressions(errors, stage.Variables);
                    CheckConditionalExpressions(errors, stage.Condition, Level.Stage);
                    if(stage.Jobs != null) {
                        foreach(var job in stage.Jobs) {
                            CheckVariableExpressions(errors, job.Variables);
                            CheckConditionalExpressions(errors, job.Condition, Level.Job);
                            CheckSingleRuntimeExpression(errors, job.ContinueOnError);
                            CheckSingleRuntimeExpression(errors, job.Strategy?.MatrixExpression);
                            CheckSingleRuntimeExpression(errors, job.Strategy?.MaxParallel);
                            CheckSingleRuntimeExpression(errors, job.Strategy?.Parallel);
                            CheckSingleRuntimeExpression(errors, job.Container?.Alias ?? job.Container?.Image);
                            if(job.Steps != null) {
                                foreach(var step in job.Steps) {
                                    CheckConditionalExpressions(errors, step.Condition, Level.Step);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void CheckVariableExpressions(TemplateValidationErrors errors, Dictionary<string, VariableValue> vars)
        {
            if(vars == null) {
                return;
            }
            foreach (var c in vars)
            {
                if (c.Value?.Value == null)
                {
                    continue;
                }
                var val = c.Value.Value;
                CheckSingleRuntimeExpression(errors, val);
            }
        }

        private static void CheckSingleRuntimeExpression(TemplateValidationErrors errors, string val)
        {
            if (val == null || !(val.StartsWith("$[") && val.EndsWith("]")))
            {
                return;
            }
            try {
                var parser = new ExpressionParser() { Flags = ExpressionFlags.DTExpressionsV1 | ExpressionFlags.ExtendedDirectives | ExpressionFlags.AllowAnyForInsert };
                var node = parser.CreateTree(val.Substring(2, val.Length - 3), new EmptyTraceWriter().ToExpressionTraceWriter(),
                    new[] { "variables", "resources", "pipeline", "dependencies", "stageDependencies" }.Select(n => new NamedValueInfo<NoOperationNamedValue>(n)),
                    ExpressionConstants.AzureWellKnownFunctions.Where(kv => kv.Key != "split").Select(kv => kv.Value).Append(new FunctionInfo<NoOperation>("counter", 0, 2)));
            } catch (Exception ex) {
                errors.Add(ex.Message);
            }
        }

        private static void CheckConditionalExpressions(TemplateValidationErrors errors, string condition, Level level)
        {
            if(condition == null) {
                return;
            }
            var val = condition;
            IEnumerable<string> names = new[] {"variables"};
            switch(level) {
            case Level.Stage:
                names = names.Concat(new[] {"pipeline", "dependencies"});
                break;
            case Level.Job:
                names = names.Concat(new[] {"pipeline", "dependencies", "stageDependencies"});
                break;
            default:
                break;
            }
            var funcs = new List<IFunctionInfo>();
            funcs.Add(new FunctionInfo<NoOperation>(PipelineTemplateConstants.Always, 0, 0));
            funcs.Add(new FunctionInfo<NoOperation>("Canceled", 0, 0));
            funcs.Add(new FunctionInfo<NoOperation>("Failed", 0, Int32.MaxValue));
            funcs.Add(new FunctionInfo<NoOperation>("Succeeded", 0, Int32.MaxValue));
            funcs.Add(new FunctionInfo<NoOperation>("SucceededOrFailed", 0, Int32.MaxValue));
            try {
                var parser = new ExpressionParser() { Flags = ExpressionFlags.DTExpressionsV1 | ExpressionFlags.ExtendedDirectives | ExpressionFlags.AllowAnyForInsert };
                var node = parser.CreateTree(val, new EmptyTraceWriter().ToExpressionTraceWriter(),
                    names.Select(n => new NamedValueInfo<NoOperationNamedValue>(n)),
                    ExpressionConstants.AzureWellKnownFunctions.Where(kv => kv.Key != "split").Select(kv => kv.Value).Concat(funcs));
            } catch (Exception ex) {
                errors.Add(ex.Message);
            }
        }
    }
}
