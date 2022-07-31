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

    public static TemplateContext CreateTemplateContext(GitHub.DistributedTask.ObjectTemplating.ITraceWriter traceWriter, IList<string> fileTable, DictionaryContextData? contextData = null) {
        var templateContext = new TemplateContext() {
            CancellationToken = CancellationToken.None,
            Errors = new TemplateValidationErrors(10, 500),
            Memory = new TemplateMemory(
                maxDepth: 100,
                maxEvents: 1000000,
                maxBytes: 10 * 1024 * 1024),
            TraceWriter = traceWriter,
            Schema = LoadSchema()
        };
        // if(exctx != null) {
        //     templateContext.State[nameof(ExecutionContext)] = exctx;
        //     templateContext.ExpressionFunctions.Add(new FunctionInfo<AlwaysFunction>(PipelineTemplateConstants.Always, 0, 0));
        //     templateContext.ExpressionFunctions.Add(new FunctionInfo<CancelledFunction>(PipelineTemplateConstants.Cancelled, 0, 0));
        //     templateContext.ExpressionFunctions.Add(new FunctionInfo<FailureFunction>(PipelineTemplateConstants.Failure, 0, Int32.MaxValue));
        //     templateContext.ExpressionFunctions.Add(new FunctionInfo<SuccessFunction>(PipelineTemplateConstants.Success, 0, Int32.MaxValue));
        // }
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<And>("and", 2, Int32.MaxValue));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<Coalesce>("coalesce", 2, Int32.MaxValue));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<Contains>("contains", 2, 2));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<ContainsValue>("containsvalue", 2, 2));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<GitHub.DistributedTask.Expressions2.Sdk.Functions.ToJson>("converttojson", 1, 1));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<EndsWith>("endsWith", 2, 2));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<Equal>("eq", 2, 2));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<GitHub.DistributedTask.Expressions2.Sdk.Functions.Format>("format", 2, Int32.MaxValue));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<GreaterThanOrEqual>("ge", 2, 2));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<GreaterThan>("gt", 2, 2));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<In>("in", 1, Int32.MaxValue));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<Join>("join", 2, 2));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<LessThanOrEqual>("le", 2, 2));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<Length>("length", 1, 1));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<Lower>("lower", 1, 1));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<LessThan>("lt", 2, 2));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<NotEqual>("ne", 2, 2));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<Not>("not", 1, 1));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<NotIn>("notin", 1, Int32.MaxValue));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<Or>("or", 2, Int32.MaxValue));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<Replace>("replace", 3, 3));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<StartsWith>("startsWith", 3, 3));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<Upper>("upper", 1, 1));
        // templateContext.ExpressionFunctions.Add(new FunctionInfo<Xor>("xor", 2, 2));
        // List<string> ctx;
        // foreach (var func in templateContext.ExpressionFunctions) {
        //     func.Name
        // }
        // foreach (var func in ExpressionConstants.WellKnownFunctions.Values) {
        //     templateContext.ExpressionFunctions.Add(func);
        // }
        if(contextData != null) {
            foreach (var pair in contextData) {
                templateContext.ExpressionValues[pair.Key] = pair.Value;
            }
        }
        foreach(var fileName in fileTable) {
            templateContext.GetFileId(fileName);
        }
        return templateContext;
    }

    public static void ParseSteps(IList<JobStep> steps, TemplateToken step, Dictionary<string, TaskMetaData> tasksByNameAndVersion, IFileProvider provider = null) {
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
                        tstep.ContinueOnError = mstep[i].Value.AssertBoolean("step value");
                    break;
                    case "enabled":
                        tstep.Enabled = mstep[i].Value.AssertBoolean("step value").Value;
                    break;
                    case "retryCountOnTaskFailure":
                        tstep.RetryCountOnTaskFailure = (int)mstep[i].Value.AssertNumber("step value").Value;
                    break;
                    case "timeoutInMinutes":
                        tstep.TimeoutInMinutes = mstep[i].Value.AssertNumber("step value");
                    break;
                    case "target":
                        tstep.Target = new StepTarget() { Target = mstep[i].Value.AssertString("step value").Value };
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

            switch(primaryKey) {
                case "task":
                    var task = primaryValue;
                    tstep.Id = Guid.NewGuid();
                    //tstep.Reference = new TaskStepDefinitionReference() { Id = Guid.Parse("e213ff0f-5d5c-4791-802d-52ea3e7be1f1"), Name = task.Split("@", 2)[0], Version = task.Split("@", 2)[1] };
                    var metaData = tasksByNameAndVersion[task];
                    tstep.Reference = new TaskStepDefinitionReference() { Id = metaData.Id, Name = metaData.Name, Version = $"{metaData.Version.Major}.{metaData.Version.Minor}.{metaData.Version.Patch}" };
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
                    // tstep.Reference = new TaskStepDefinitionReference() { Id = Guid.Parse("e213ff0f-5d5c-4791-802d-52ea3e7be1f1"), Name = "PowerShell", Version = "2.180.1" };
                    metaData = tasksByNameAndVersion["PowerShell@2"];
                    tstep.Reference = new TaskStepDefinitionReference() { Id = metaData.Id, Name = metaData.Name, Version = $"{metaData.Version.Major}.{metaData.Version.Minor}.{metaData.Version.Patch}" };
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
                    // tstep.Reference = new TaskStepDefinitionReference() { Id = Guid.Parse("e213ff0f-5d5c-4791-802d-52ea3e7be1f1"), Name = "PowerShell", Version = "2.180.1" };
                    metaData = tasksByNameAndVersion["Bash@3"];
                    tstep.Reference = new TaskStepDefinitionReference() { Id = metaData.Id, Name = metaData.Name, Version = $"{metaData.Version.Major}.{metaData.Version.Minor}.{metaData.Version.Patch}" };
                    for(int i = 0; i < unparsedTokens.Count; i++) {
                        tstep.Inputs[unparsedTokens[i].Key.AssertString("step key").Value] = unparsedTokens[i].Value.AssertString("step key").Value;
                    }
                    tstep.Inputs["targetType"] = "inline";
                    tstep.Inputs["script"] = primaryValue;
                    steps.Add(tstep);
                break;
                case "script":
                    tstep.Id = Guid.NewGuid();
                    // tstep.Reference = new TaskStepDefinitionReference() { Id = Guid.Parse("e213ff0f-5d5c-4791-802d-52ea3e7be1f1"), Name = "PowerShell", Version = "2.180.1" };
                    // Seems to be Bash@3 for non windows
                    metaData = tasksByNameAndVersion["CmdLine@2"];
                    tstep.Reference = new TaskStepDefinitionReference() { Id = metaData.Id, Name = metaData.Name, Version = $"{metaData.Version.Major}.{metaData.Version.Minor}.{metaData.Version.Patch}" };
                    for(int i = 0; i < unparsedTokens.Count; i++) {
                        tstep.Inputs[unparsedTokens[i].Key.AssertString("step key").Value] = unparsedTokens[i].Value.AssertString("step key").Value;
                    }
                    tstep.Inputs["targetType"] = "inline";
                    tstep.Inputs["script"] = primaryValue;
                    steps.Add(tstep);
                break;
                case "template":
                    var file = ReadTemplate(provider, primaryValue, unparsedTokens.Count == 1 ? unparsedTokens[0].Value.AssertMapping("param").ToDictionary(kv => kv.Key.AssertString("").Value, kv => kv.Value) : null);
                    foreach(var step2 in (from e in file where e.Key.AssertString("").Value == "steps" select e.Value).First().AssertSequence("")) {
                        ParseSteps(steps, step2, tasksByNameAndVersion, provider);
                    }
                break;
                default:
                throw new Exception("Syntax Error");
            }
        } else {
            throw new Exception("Syntax Error");
        }
    }

    public static MappingToken ReadTemplate(IFileProvider provider, string filename, Dictionary<string, TemplateToken>? cparameters = null) {
        var templateContext = AzureDevops.CreateTemplateContext(new EmptyTraceWriter(), new List<string>());
        var fileId = templateContext.GetFileId(filename);
        // Read the file
        var fileContent = System.IO.File.ReadAllText(filename);

        TemplateToken token;
        using (var stringReader = new StringReader(fileContent))
        {
            var yamlObjectReader = new YamlObjectReader(fileId, stringReader);
            token = TemplateReader.Read(templateContext, "workflow-root", yamlObjectReader, fileId, out _);
        }

        templateContext.Errors.Check();

        var pipelineroot = token.AssertMapping("root");

        TemplateToken? parameters = null;
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
        contextData["variables"] = null;
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
        }

        templateContext = AzureDevops.CreateTemplateContext(new EmptyTraceWriter(), templateContext.GetFileTable().ToArray(), contextData);

        return TemplateEvaluator.Evaluate(templateContext, "workflow-root", pipelineroot, 0, fileId).AssertMapping("root");
    }
}