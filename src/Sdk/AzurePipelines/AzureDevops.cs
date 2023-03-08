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

    public static TemplateContext CreateTemplateContext(GitHub.DistributedTask.ObjectTemplating.ITraceWriter traceWriter, IList<string> fileTable, ExpressionFlags flags, DictionaryContextData contextData = null) {
        var templateContext = new TemplateContext {
            Flags = flags,
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

    public static void ParseVariables(Runner.Server.Azure.Devops.Context context, IDictionary<string, VariableValue> vars, TemplateToken rawvars, bool onlyStaticVars = false) {
        if(rawvars is MappingToken mvars) {
            foreach(var kv in mvars) {
                // Skip expressions if we parse static variables
                if(onlyStaticVars && (kv.Key is ExpressionToken || kv.Value is ExpressionToken)) {
                    continue;
                }
                vars[kv.Key.AssertString("variables").Value] = kv.Value.AssertLiteralString("variables");
            }
        } else {
            foreach(var rawdef in rawvars.AssertSequence("")) {
                string template = null;
                TemplateToken parameters = null;
                string name = null;
                string value = null;
                bool isReadonly = false;
                string group = null;
                bool skip = false;
                foreach(var kv in rawdef.AssertMapping("")) {
                    if(onlyStaticVars && (kv.Key is ExpressionToken || kv.Value is ExpressionToken)) {
                        skip = true;
                        break;
                    }
                    var primaryKey = kv.Key.AssertString("variables").Value;
                    switch(primaryKey) {
                        case "template":
                            template = kv.Value.AssertString("variables").Value;
                        break;
                        case "parameters":
                            parameters = kv.Value;
                        break;
                        case "name":
                            name = kv.Value.AssertLiteralString("variables");
                        break;
                        case "value":
                            value = kv.Value.AssertLiteralString("variables");
                        break;
                        case "readonly":
                            isReadonly = kv.Value.AssertAzurePipelinesBoolean("variables");
                        break;
                        case "group":
                            group = kv.Value.AssertLiteralString("variables");
                        break;
                    }
                }
                // Skip expressions and template references if we parse static variables
                if(skip || onlyStaticVars && template != null) {
                    continue;
                }
                if(group != null) {
                    var groupVars = context.VariablesProvider?.GetVariablesForEnvironment(group);
                    if(groupVars != null) {
                        foreach(var v in groupVars) {
                            vars[v.Key] = new VariableValue(v.Value);
                        }
                    }
                } else if(template != null) {
                    var file = ReadTemplate(context, template, parameters != null ? parameters.AssertMapping("param").ToDictionary(kv => kv.Key.AssertString("").Value, kv => kv.Value) : null, "variable-template-root");
                    ParseVariables(context.ChildContext(file, template), vars, (from e in file where e.Key.AssertString("").Value == "variables" select e.Value).First());
                } else {
                    vars[name] = new VariableValue(value, isReadonly: isReadonly);
                }
            }
        }
    }
    public static void ParseSteps(Runner.Server.Azure.Devops.Context context, IList<TaskStep> steps, TemplateToken step) {
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
                        tstep.Condition = mstep[i].Value.AssertLiteralString("step value");
                    break;
                    case "continueOnError":
                        tstep.ContinueOnError = new BooleanToken(null, null, null, mstep[i].Value.AssertAzurePipelinesBoolean("step value"));
                    break;
                    case "enabled":
                        tstep.Enabled = mstep[i].Value.AssertAzurePipelinesBoolean("step value");
                    break;
                    case "retryCountOnTaskFailure":
                        tstep.RetryCountOnTaskFailure = Int32.Parse(mstep[i].Value.AssertLiteralString("step value"));
                    break;
                    case "timeoutInMinutes":
                        tstep.TimeoutInMinutes = new NumberToken(null, null, null, Int32.Parse(mstep[i].Value.AssertLiteralString("step value")));
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
                if(context.TaskByNameAndVersion != null) {
                    var metaData = context.TaskByNameAndVersion.Resolve(task);
                    if(metaData == null) {
                        throw new Exception($"Failed to resolve task {task}");
                    }
                    return new TaskStepDefinitionReference() { Id = metaData.Id, Name = metaData.Name, Version = $"{metaData.Version.Major}.{metaData.Version.Minor}.{metaData.Version.Patch}", RawNameAndVersion = task };
                }
                return new TaskStepDefinitionReference() { RawNameAndVersion = task };
            };
            Action addUnparsedTokensAsInputs = () => {
                for(int i = 0; i < unparsedTokens.Count; i++) {
                    tstep.Inputs[unparsedTokens[i].Key.AssertString("step key").Value] = unparsedTokens[i].Value.AssertLiteralString("step key");
                }
            };
            tstep.Id = Guid.NewGuid();
            switch(primaryKey) {
                case "task":
                    var task = primaryValue;
                    tstep.Reference = nameToReference(task);
                    for(int i = 0; i < unparsedTokens.Count; i++) {
                        switch(unparsedTokens[i].Key.AssertString("step key").Value) {
                            case "inputs":
                                foreach(var kv in unparsedTokens[i].Value.AssertMapping("inputs mapping")) {
                                    tstep.Inputs[kv.Key.AssertString("inputs key").Value] = kv.Value.AssertLiteralString("inputs value");
                                }
                                break;
                        }
                    }
                    steps.Add(tstep);
                break;
                case "powershell":
                case "pwsh":
                    tstep.Reference = nameToReference("PowerShell@2");
                    addUnparsedTokensAsInputs();
 
                    tstep.Inputs["targetType"] = "inline";
                    if(primaryKey == "pwsh") {
                        tstep.Inputs["pwsh"] = "true";
                    }
                    tstep.Inputs["script"] = primaryValue;
                    steps.Add(tstep);
                break;
                case "bash":
                    tstep.Reference = nameToReference("Bash@3");
                    addUnparsedTokensAsInputs();
                    tstep.Inputs["targetType"] = "inline";
                    tstep.Inputs["script"] = primaryValue;
                    steps.Add(tstep);
                break;
                case "script":
                    tstep.Reference = nameToReference("CmdLine@2");
                    addUnparsedTokensAsInputs();
                    tstep.Inputs["script"] = primaryValue;
                    steps.Add(tstep);
                break;
                case "checkout":
                    tstep.Reference = nameToReference("Checkout@1");
                    addUnparsedTokensAsInputs();
                    tstep.Inputs["repository"] = primaryValue;
                    steps.Add(tstep);
                break;
                case "download":
                    tstep.Reference = nameToReference("DownloadPipelineArtifact@2");
                    addUnparsedTokensAsInputs();
                    tstep.Inputs["buildType"] = primaryValue;
                    steps.Add(tstep);
                break;
                case "downloadBuild":
                    tstep.Reference = nameToReference("DownloadBuildArtifacts@0");
                    addUnparsedTokensAsInputs();
                    tstep.Inputs["buildType"] = primaryValue;
                    steps.Add(tstep);
                break;
                case "getPackage":
                    tstep.Reference = nameToReference("DownloadPackage@1");
                    addUnparsedTokensAsInputs();
                    // Unknown if this is correct...
                    tstep.Inputs["definition"] = primaryValue;
                    steps.Add(tstep);
                break;
                case "publish":
                    tstep.Reference = nameToReference("PublishPipelineArtifact@1");
                    addUnparsedTokensAsInputs();
                    tstep.Inputs["path"] = primaryValue;
                    steps.Add(tstep);
                break;
                case "reviewApp":
                    tstep.Reference = nameToReference("ReviewApp@0");
                    addUnparsedTokensAsInputs();
                    tstep.Inputs["resourceName"] = primaryValue;
                    steps.Add(tstep);
                break;
                case "template":
                    var file = ReadTemplate(context, primaryValue, unparsedTokens.Count == 1 ? unparsedTokens[0].Value.AssertMapping("param").ToDictionary(kv => kv.Key.AssertString("").Value, kv => kv.Value) : null, "step-template-root");
                    foreach(var step2 in (from e in file where e.Key.AssertString("").Value == "steps" select e.Value).First().AssertSequence("")) {
                        ParseSteps(context.ChildContext(file, primaryValue), steps, step2);
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
            ParseSteps(context, steps, val);
            return steps[0].ToContextData();
            case "stepList":
            if(val.Type == TokenType.Null) {
                return new ArrayContextData();
            }
            foreach(var step2 in val.AssertSequence("")) {
                ParseSteps(context, steps, step2);
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
            return new Job().Parse(context, val).ToContextData();
            case "jobList":
            if(val.Type == TokenType.Null) {
                return new ArrayContextData();
            }
            Job.ParseJobs(context, jobs, val.AssertSequence(""));
            var jobList = new ArrayContextData();
            foreach(var job in jobs) {
                jobList.Add(job.ToContextData());
            }
            return jobList;
            case "deployment":
            if(val.Type == TokenType.Null) {
                return null;
            }
            var djob = new Job().Parse(context, val);
            if(!djob.DeploymentJob) throw new Exception("Only Deployment Jobs are valid");
            return djob.ToContextData();
            case "deploymentList":
            if(val.Type == TokenType.Null) {
                return new ArrayContextData();
            }
            Job.ParseJobs(context, jobs, val.AssertSequence(""));
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
            return new Stage().Parse(context, val).ToContextData();
            case "stageList":
            if(val.Type == TokenType.Null) {
                return new ArrayContextData();
            }
            Stage.ParseStages(context, stages, val.AssertSequence(""));
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

    public static MappingToken ReadTemplate(Runner.Server.Azure.Devops.Context context, string filenameAndRef, Dictionary<string, TemplateToken> cparameters = null, string schemaName = null) {
        var variables = context.VariablesProvider?.GetVariablesForEnvironment("");
        var templateContext = AzureDevops.CreateTemplateContext(context.TraceWriter ?? new EmptyTraceWriter(), new List<string>(), context.Flags);
        var afilenameAndRef = filenameAndRef.Split("@", 2);
        var filename = afilenameAndRef[0];
        var fileId = templateContext.GetFileId(filename);
        // Read the file
        var fileContent = context.FileProvider.ReadFile(afilenameAndRef.Length == 1 ? context.RepositoryAndRef : string.Equals(afilenameAndRef[1], "self", StringComparison.OrdinalIgnoreCase) ? null : context.Repositories[afilenameAndRef[1]], afilenameAndRef.Length == 1 ? RelativeTo(context.CWD ?? ".", filename) : filename);

        TemplateToken token;
        using (var stringReader = new StringReader(fileContent))
        {
            // preserveString is needed for azure pipelines compatability of the templateContext property all boolean and number token are casted to string without loosing it's exact string value
            var yamlObjectReader = new YamlObjectReader(fileId, stringReader, preserveString: true, forceAzurePipelines: true);
            token = TemplateReader.Read(templateContext, schemaName ?? "pipeline-root", yamlObjectReader, fileId, out _);
        }

        templateContext.Errors.Check();

        var pipelineroot = token.AssertMapping("root");

        TemplateToken parameters = null;
        TemplateToken rawStaticVariables = null;
        foreach(var kv in pipelineroot) {
            if(kv.Key.Type != TokenType.String) {
                continue;
            }
            switch((kv.Key as StringToken)?.Value) {
                case "parameters":
                parameters = kv.Value;
                break;
                case "variables":
                rawStaticVariables = kv.Value;
                break;
            }
        }

        var contextData = new DictionaryContextData();
        var parametersData = new DictionaryContextData();
        contextData["parameters"] = parametersData;
        var variablesData = new DictionaryContextData();
        contextData["variables"] = variablesData;
        if(variables != null) {
            foreach(var v in variables) {
                variablesData[v.Key] = new StringContextData(v.Value);
            }
        }
        if(rawStaticVariables != null) {
            IDictionary<string, VariableValue> pvars = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
            ParseVariables(context, pvars, rawStaticVariables, true);
            foreach(var v in pvars) {
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
        } else {
            if(cparameters != null && 0 != cparameters.Count) {
                throw new Exception("Provided undeclared parameters");
            }
        }

        templateContext = AzureDevops.CreateTemplateContext(context.TraceWriter ?? new EmptyTraceWriter(), templateContext.GetFileTable().ToArray(), context.Flags, contextData);

        var evaluatedResult = TemplateEvaluator.Evaluate(templateContext, schemaName ?? "pipeline-root", pipelineroot, 0, fileId);
        templateContext.Errors.Check();
        return evaluatedResult.AssertMapping("root");
    }

    public static TemplateToken ConvertAllScalarsToString(TemplateToken token) {
        if(token is MappingToken map) {
            for(int i = 0; i < map.Count; i++) {
                map[i] = new KeyValuePair<ScalarToken, TemplateToken>(map[i].Key, ConvertAllScalarsToString(map[i].Value));
            }
            return token;
        } else if(token is SequenceToken seq) {
            for(int i = 0; i < seq.Count; i++) {
                seq[i] = ConvertAllScalarsToString(seq[i]);
            }
            return token;
        } else {
            return new StringToken(token.FileId, token.Line, token.Column, token.ToString());
        }
    }
}
