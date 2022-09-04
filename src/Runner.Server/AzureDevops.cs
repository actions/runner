using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Expressions2.Sdk.Functions.v1;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Schema;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.WebApi;
using Runner.Server.Azure.Devops;

public class AzureDevops {

    private static TemplateSchema LoadSchema() {
        var assembly = Assembly.GetExecutingAssembly();
        var json = default(String);
        using (var stream = assembly.GetManifestResourceStream("azurepiplines.json"))
        using (var streamReader = new StreamReader(stream))
        {
            json = streamReader.ReadToEnd();
        }

        var objectReader = new JsonObjectReader(null, json);
        return TemplateSchema.Load(objectReader);
    }

    public static TemplateContext CreateTemplateContext(GitHub.DistributedTask.ObjectTemplating.ITraceWriter traceWriter, IList<string> fileTable, DictionaryContextData contextData = null) {
        var templateContext = new TemplateContext(true) {
            CancellationToken = CancellationToken.None,
            Errors = new TemplateValidationErrors(10, 500),
            Memory = new TemplateMemory(
                maxDepth: 100,
                maxEvents: 1000000,
                maxBytes: 10 * 1024 * 1024),
            TraceWriter = traceWriter,
            Schema = LoadSchema()
        };
        if(contextData != null) {
            foreach (var pair in contextData) {
                templateContext.ExpressionValues[pair.Key] = pair.Value;
            }
        }
        if(fileTable != null) {
            foreach(var fileName in fileTable) {
                templateContext.GetFileId(fileName);
            }
        }
        return templateContext;
    }

    public static void ParseVariables(Runner.Server.Azure.Devops.Context context, IDictionary<string, VariableValue> vars, TemplateToken rawvars) {
        if(rawvars is MappingToken mvars) {
            foreach(var kv in mvars) {
                vars[kv.Key.AssertString("variables").Value] = kv.Value.AssertString("variables").Value;
            }
        } else {
            foreach(var rawdef in rawvars.AssertSequence("")) {
                string template = null;
                TemplateToken parameters = null;
                string name = null;
                string value = null;
                bool isReadonly = false;
                foreach(var kv in rawdef.AssertMapping("")) {
                    var primaryKey = kv.Key.AssertString("variables").Value;
                    switch(primaryKey) {
                        case "template":
                            template = kv.Value.AssertString("variables").Value;
                        break;
                        case "parameters":
                            parameters = kv.Value;
                        break;
                        case "name":
                            name = kv.Value.AssertString("variables").Value;
                        break;
                        case "value":
                            value = kv.Value.AssertString("variables").Value;
                        break;
                        case "readonly":
                            isReadonly = kv.Value.AssertBoolean("variables").Value;
                        break;
                    }
                }
                if(template != null) {
                    var file = ReadTemplate(context, template, parameters != null ? parameters.AssertMapping("param").ToDictionary(kv => kv.Key.AssertString("").Value, kv => kv.Value) : null);
                    ParseVariables(context.ChildContext(file, template), vars, (from e in file where e.Key.AssertString("").Value == "variables" select e.Value).First());
                } else {
                    vars[name] = new VariableValue(value, isReadonly: isReadonly);
                }
            }
        }
    }
    public static void ParseSteps(Runner.Server.Azure.Devops.Context context, IList<TaskStep> steps, TemplateToken step, Dictionary<string, TaskMetaData> tasksByNameAndVersion) {
        if(step is MappingToken mstep && mstep.Count >= 1) {
            var tstep = new TaskStep();
            MappingToken unparsedTokens = new MappingToken(null, null, null);
            var primaryKey = mstep[0].Key.ToString();
            var isTemplate = primaryKey == "template";
            for(int i = 1; i < mstep.Count; i++) {
                if(isTemplate) {
                    unparsedTokens.Add(mstep[i]);
                    continue;
                }
                switch(mstep[i].Key.AssertString("step key").Value) {
                    case "condition":
                        tstep.Condition = mstep[i].Value.AssertString("step value").Value;
                    break;
                    case "continueOnError":
                        tstep.ContinueOnError = mstep[i].Value.AssertBoolean("step value").Clone(true);
                    break;
                    case "enabled":
                        tstep.Enabled = mstep[i].Value.AssertBoolean("step value").Value;
                    break;
                    case "retryCountOnTaskFailure":
                        tstep.RetryCountOnTaskFailure = (int)mstep[i].Value.AssertNumber("step value").Value;
                    break;
                    case "timeoutInMinutes":
                        tstep.TimeoutInMinutes = mstep[i].Value.AssertNumber("step value").Clone(true);
                    break;
                    case "target":
                        if(mstep[i].Value is StringToken targetStr) {
                            tstep.Target = new StepTarget() { Target = targetStr.Value };
                        } else {
                            tstep.Target = new StepTarget();
                            foreach(var targetKv in mstep[i].Value.AssertMapping("target mapping")) {
                                switch((targetKv.Key as StringToken).Value) {
                                    case "container":
                                        tstep.Target.Target = (targetKv.Value as StringToken)?.Value;
                                    break;
                                    case "commands":
                                        tstep.Target.Commands = (targetKv.Value as StringToken)?.Value;
                                    break;
                                    case "settableVariables":
                                        tstep.Target.SettableVariables = new TaskVariableRestrictions();
                                        if((targetKv.Value as StringToken)?.Value != "none") {
                                            foreach(var svar in targetKv.Value.AssertSequence("SettableVariables sequence")) {
                                                tstep.Target.SettableVariables.Allowed.Add(svar.AssertString("").Value);
                                            }
                                        }
                                    break;
                                }
                            }
                        }
                    break;
                    case "env":
                        foreach(var kv in mstep[i].Value.AssertMapping("env mapping")) {
                            tstep.Environment[kv.Key.AssertString("env key").Value] = kv.Value.AssertString("env value").Value;
                        }
                    break;
                    case "name":
                        tstep.Name = mstep[i].Value.AssertString("step value").Value;
                    break;
                    case "displayName":
                        tstep.DisplayName = mstep[i].Value.AssertString("step value").Value;
                    break;
                    default:
                        unparsedTokens.Add(mstep[i]);
                    break;
                }
            }
            var primaryValue = mstep[0].Value.AssertString("step value").Value;
            Func<string, TaskStepDefinitionReference> nameToReference = task => {
                if(string.Equals(task, "Checkout@1", StringComparison.OrdinalIgnoreCase) || string.Equals(task, "6d15af64-176c-496d-b583-fd2ae21d4df4@1", StringComparison.OrdinalIgnoreCase) || string.Equals(task, "Checkout@1.0.0", StringComparison.OrdinalIgnoreCase) || string.Equals(task, "6d15af64-176c-496d-b583-fd2ae21d4df4@1.0.0", StringComparison.OrdinalIgnoreCase)) {
                    return new TaskStepDefinitionReference {
                        Id = Guid.Parse("6d15af64-176c-496d-b583-fd2ae21d4df4"),
                        Name = "Checkout",
                        Version = "1.0.0",
                        RawNameAndVersion = task
                    };
                }
                if(tasksByNameAndVersion != null) {
                    var metaData = tasksByNameAndVersion[task];
                    return new TaskStepDefinitionReference() { Id = metaData.Id, Name = metaData.Name, Version = $"{metaData.Version.Major}.{metaData.Version.Minor}.{metaData.Version.Patch}", RawNameAndVersion = task };
                }
                return new TaskStepDefinitionReference() { RawNameAndVersion = task };
            };
            switch(primaryKey) {
                case "task":
                    var task = primaryValue;
                    tstep.Id = Guid.NewGuid();
                    tstep.Reference = nameToReference(task);
                    for(int i = 0; i < unparsedTokens.Count; i++) {
                        switch(unparsedTokens[i].Key.AssertString("step key").Value) {
                            case "inputs":
                                foreach(var kv in unparsedTokens[i].Value.AssertMapping("inputs mapping")) {
                                    tstep.Inputs[kv.Key.AssertString("inputs key").Value] = kv.Value.AssertString("inputs value").Value;
                                }
                                break;
                        }
                    }
                    steps.Add(tstep);
                break;
                case "powershell":
                case "pwsh":
                    tstep.Id = Guid.NewGuid();
                    tstep.Reference = nameToReference("PowerShell@2");
                    for(int i = 0; i < unparsedTokens.Count; i++) {
                        tstep.Inputs[unparsedTokens[i].Key.AssertString("step key").Value] = unparsedTokens[i].Value.AssertString("step key").Value;
                    }
                    tstep.Inputs["targetType"] = "inline";
                    if(primaryKey == "pwsh") {
                        tstep.Inputs["pwsh"] = "true";
                    }
                    tstep.Inputs["script"] = primaryValue;
                    steps.Add(tstep);
                break;
                case "bash":
                    tstep.Id = Guid.NewGuid();
                    tstep.Reference = nameToReference("Bash@3");
                    for(int i = 0; i < unparsedTokens.Count; i++) {
                        tstep.Inputs[unparsedTokens[i].Key.AssertString("step key").Value] = unparsedTokens[i].Value.AssertString("step key").Value;
                    }
                    tstep.Inputs["targetType"] = "inline";
                    tstep.Inputs["script"] = primaryValue;
                    steps.Add(tstep);
                break;
                case "script":
                    tstep.Id = Guid.NewGuid();
                    tstep.Reference = nameToReference("CmdLine@2");
                    for(int i = 0; i < unparsedTokens.Count; i++) {
                        tstep.Inputs[unparsedTokens[i].Key.AssertString("step key").Value] = unparsedTokens[i].Value.AssertString("step key").Value;
                    }
                    tstep.Inputs["script"] = primaryValue;
                    steps.Add(tstep);
                break;
                case "checkout":
                    tstep.Id = Guid.NewGuid();
                    tstep.Reference = nameToReference("Checkout@1");
                    for(int i = 0; i < unparsedTokens.Count; i++) {
                        tstep.Inputs[unparsedTokens[i].Key.AssertString("step key").Value] = unparsedTokens[i].Value.AssertString("step key").Value;
                    }
                    tstep.Inputs["repository"] = primaryValue;
                    steps.Add(tstep);
                break;
                case "download":
                    tstep.Id = Guid.NewGuid();
                    tstep.Reference = nameToReference("DownloadPipelineArtifact@2");
                    for(int i = 0; i < unparsedTokens.Count; i++) {
                        tstep.Inputs[unparsedTokens[i].Key.AssertString("step key").Value] = unparsedTokens[i].Value.AssertString("step key").Value;
                    }
                    tstep.Inputs["source"] = primaryValue;
                    steps.Add(tstep);
                break;
                case "downloadBuild":
                    tstep.Id = Guid.NewGuid();
                    tstep.Reference = nameToReference("DownloadBuildArtifacts@0");
                    for(int i = 0; i < unparsedTokens.Count; i++) {
                        tstep.Inputs[unparsedTokens[i].Key.AssertString("step key").Value] = unparsedTokens[i].Value.AssertString("step key").Value;
                    }
                    // Unknown if this is correct...
                    tstep.Inputs["definition"] = primaryValue;
                    steps.Add(tstep);
                break;
                case "getPackage":
                    tstep.Id = Guid.NewGuid();
                    tstep.Reference = nameToReference("DownloadPackage@1");
                    for(int i = 0; i < unparsedTokens.Count; i++) {
                        tstep.Inputs[unparsedTokens[i].Key.AssertString("step key").Value] = unparsedTokens[i].Value.AssertString("step key").Value;
                    }
                    // Unknown if this is correct...
                    tstep.Inputs["definition"] = primaryValue;
                    steps.Add(tstep);
                break;
                case "publish":
                    tstep.Id = Guid.NewGuid();
                    tstep.Reference = nameToReference("PublishPipelineArtifact@1");
                    for(int i = 0; i < unparsedTokens.Count; i++) {
                        tstep.Inputs[unparsedTokens[i].Key.AssertString("step key").Value] = unparsedTokens[i].Value.AssertString("step key").Value;
                    }
                    tstep.Inputs["path"] = primaryValue;
                    steps.Add(tstep);
                break;
                case "reviewApp":
                    tstep.Id = Guid.NewGuid();
                    tstep.Reference = nameToReference("ReviewApp@0");
                    for(int i = 0; i < unparsedTokens.Count; i++) {
                        tstep.Inputs[unparsedTokens[i].Key.AssertString("step key").Value] = unparsedTokens[i].Value.AssertString("step key").Value;
                    }
                    tstep.Inputs["resourceName"] = primaryValue;
                    steps.Add(tstep);
                break;
                case "template":
                    var file = ReadTemplate(context, primaryValue, unparsedTokens.Count == 1 ? unparsedTokens[0].Value.AssertMapping("param").ToDictionary(kv => kv.Key.AssertString("").Value, kv => kv.Value) : null);
                    foreach(var step2 in (from e in file where e.Key.AssertString("").Value == "steps" select e.Value).First().AssertSequence("")) {
                        ParseSteps(context.ChildContext(file, primaryValue), steps, step2, tasksByNameAndVersion);
                    }
                break;
                default:
                throw new Exception("Syntax Error");
            }
        } else {
            throw new Exception("Syntax Error");
        }
    }

    private static PipelineContextData ConvertValue(Runner.Server.Azure.Devops.Context context, TemplateToken val, string type) {
        var steps = new List<TaskStep>();
        var jobs = new List<Job>();
        var stages = new List<Stage>();
        switch(type) {
            case "object":
            return val.ToContextData();
            case "boolean":
            return val.Type == TokenType.Null ? new BooleanContextData(false) : val.ToContextData().AssertBoolean("boolean type");
            case "number":
            return val.Type == TokenType.Null ? new NumberContextData(0) : val.ToContextData().AssertNumber("number type");
            case "string":
            return val.Type == TokenType.Null ? new StringContextData("") : val.ToContextData().AssertString("string type");
            case "step":
            if(val.Type == TokenType.Null) {
                return null;
            }
            ParseSteps(context, steps, val, null);
            return steps[0].ToContextData();
            case "stepList":
            if(val.Type == TokenType.Null) {
                return new ArrayContextData();
            }
            foreach(var step2 in val.AssertSequence("")) {
                ParseSteps(context, steps, step2, null);
            }
            var stepList = new ArrayContextData();
            foreach(var step in steps) {
                stepList.Add(step.ToContextData());
            }
            return stepList;
            case "job":
            if(val.Type == TokenType.Null) {
                return null;
            }
            return new Job().Parse(context, val, null).ToContextData();
            case "jobList":
            if(val.Type == TokenType.Null) {
                return new ArrayContextData();
            }
            Job.ParseJobs(context, jobs, val.AssertSequence(""), null);
            var jobList = new ArrayContextData();
            foreach(var job in jobs) {
                jobList.Add(job.ToContextData());
            }
            return jobList;
            case "deployment":
            if(val.Type == TokenType.Null) {
                return null;
            }
            var djob = new Job().Parse(context, val, null);
            if(!djob.DeploymentJob) throw new Exception("Only Deployment Jobs are valid");
            return djob.ToContextData();
            case "deploymentList":
            if(val.Type == TokenType.Null) {
                return new ArrayContextData();
            }
            Job.ParseJobs(context, jobs, val.AssertSequence(""), null);
            var djobList = new ArrayContextData();
            foreach(var job in jobs) {
                if(!job.DeploymentJob) throw new Exception("Only Deployment Jobs are valid");
                djobList.Add(job.ToContextData());
            }
            return djobList;
            case "stage":
            if(val.Type == TokenType.Null) {
                return null;
            }
            return new Stage().Parse(context, val, null).ToContextData();
            case "stageList":
            if(val.Type == TokenType.Null) {
                return new ArrayContextData();
            }
            Stage.ParseStages(context, stages, val.AssertSequence(""), null);
            var stageList = new ArrayContextData();
            foreach(var stage in stages) {
                stageList.Add(stage.ToContextData());
            }
            return stageList;
            default:
            throw new Exception("This parameter type is not supported: " + type);
        }
    }

    public static string RelativeTo(string cwd, string filename) {
        var path = $"{cwd}/{filename}".Split("/").ToList();
        for(int i = 0; i < path.Count; i++) {
            if(path[i] == "." || string.IsNullOrEmpty(path[i])) {
                path.RemoveAt(i);
                i -= 1;
            } else if(path[i] == "..") {
                path.RemoveAt(i); // Remove ..
                path.RemoveAt(i - 1); // Remove the previous path component
                i -= 2;
            }
        }
        return string.Join('/', path.ToArray());
    }

    public static MappingToken ReadTemplate(Runner.Server.Azure.Devops.Context context, string filenameAndRef, Dictionary<string, TemplateToken> cparameters = null) {
        var variables = context.Variables;
        var templateContext = AzureDevops.CreateTemplateContext(context.TraceWriter ?? new EmptyTraceWriter(), new List<string>());
        var afilenameAndRef = filenameAndRef.Split("@", 2);
        var filename = afilenameAndRef[0];
        var fileId = templateContext.GetFileId(filename);
        // Read the file
        var fileContent = context.FileProvider.ReadFile(afilenameAndRef.Length == 1 ? context.RepositoryAndRef : string.Equals(afilenameAndRef[1], "self", StringComparison.OrdinalIgnoreCase) ? null : context.Repositories[afilenameAndRef[1]], afilenameAndRef.Length == 1 ? RelativeTo(context.CWD ?? ".", filename) : filename);

        TemplateToken token;
        using (var stringReader = new StringReader(fileContent))
        {
            var yamlObjectReader = new YamlObjectReader(fileId, stringReader);
            token = TemplateReader.Read(templateContext, "workflow-root", yamlObjectReader, fileId, out _);
        }

        templateContext.Errors.Check();

        var pipelineroot = token.AssertMapping("root");

        TemplateToken parameters = null;
        foreach(var kv in pipelineroot) {
            if(kv.Key.Type != TokenType.String) {
                continue;
            }
            switch((kv.Key as StringToken)?.Value) {
                case "parameters":
                parameters = kv.Value;
                break;
            }
        }

        var contextData = new DictionaryContextData();
        var parametersData = new DictionaryContextData();
        contextData["parameters"] = parametersData;
        if(variables == null) {
            contextData["variables"] = null;
        } else {
            var variablesData = new DictionaryContextData();
            contextData["variables"] = variablesData;
            foreach(var v in variables) {
                variablesData[v.Key] = new StringContextData(v.Value.Value);
            }
        }
        
        if(parameters?.Type == TokenType.Mapping) {
            int providedParameter = 0;
            foreach(var kv in parameters as MappingToken) {
                if(kv.Key.Type != TokenType.String) {
                    continue;
                }
                var paramname = (kv.Key as StringToken)?.Value;
                if(cparameters?.TryGetValue(paramname, out var value) == true) {
                    parametersData[paramname] = value.ToContextData();
                    providedParameter++;
                } else {
                    parametersData[paramname] = kv.Value.ToContextData();
                }
            }
            if(cparameters != null && providedParameter != cparameters?.Count) {
                throw new Exception("Provided undeclared parameters");
            }
        } else if(parameters is SequenceToken sparameters) {
            int providedParameter = 0;
            foreach(var mparam in sparameters) {
                var varm = mparam.AssertMapping("varm");
                string name = null;
                string type = "object";
                TemplateToken def = new NullToken(null, null, null);
                foreach(var kv in varm) {
                    switch((kv.Key as StringToken).Value) {
                        case "name":
                            name = (kv.Value as StringToken).Value;
                        break;
                        case "type":
                            type = (kv.Value as StringToken).Value;
                        break;
                        case "default":
                            def = kv.Value;
                        break;
                    }
                }
                var defCtxData = ConvertValue(context, def, type);
                if(cparameters?.TryGetValue(name, out var value) == true) {
                    parametersData[name] = ConvertValue(context, value, type);
                    providedParameter++;
                } else {
                    parametersData[name] = defCtxData;
                }
            }
            if(cparameters != null && providedParameter != cparameters?.Count) {
                throw new Exception("Provided undeclared parameters");
            }
        }

        templateContext = AzureDevops.CreateTemplateContext(context.TraceWriter ?? new EmptyTraceWriter(), templateContext.GetFileTable().ToArray(), contextData);

        var evaluatedResult = TemplateEvaluator.Evaluate(templateContext, "workflow-root", pipelineroot, 0, fileId);
        templateContext.Errors.Check();
        return evaluatedResult.AssertMapping("root");
    }
}