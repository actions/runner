using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.Expressions2.Sdk.Functions;
using GitHub.DistributedTask.Expressions2.Tokens;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Schema;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.WebApi;
using Runner.Server.Azure.Devops;

namespace Runner.Server.Azure.Devops {

    public class AzureDevops {

        public static TemplateSchema LoadSchema() {
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

        public static async Task<TemplateToken> ParseVariables(Runner.Server.Azure.Devops.Context context, IDictionary<string, VariableValue> vars, TemplateToken rawvars, TemplateContext staticVarCtx = null) {
            DictionaryContextData staticVars = null;
            if(staticVarCtx != null && staticVarCtx.ExpressionValues.TryGetValue("variables", out var rvars) && rvars is GitHub.DistributedTask.Expressions2.Sdk.IReadOnlyObject tvars) {
                staticVars = new DictionaryContextData();
                foreach (var k in tvars.Keys) {
                    var v = tvars[k] as GitHub.DistributedTask.Expressions2.Sdk.IString;
                    staticVars[k] = new StringContextData(v.GetString());
                }
                staticVarCtx.ExpressionValues["variables"] = staticVars;
            }
            if(rawvars is MappingToken mvars)
            {
                var map = new MappingToken(rawvars.FileId, rawvars.Line, rawvars.Column);
                foreach(var x in ProcessVariableMapping(vars, staticVarCtx, staticVars, mvars)) {
                    map.Add(x);
                }
                return map;
            }
            else if(rawvars is SequenceToken svars)
            {
                var seq = new SequenceToken(rawvars.FileId, rawvars.Line, rawvars.Column);
                await foreach(var x in ProcessVariableSequence(context, vars, staticVarCtx, staticVars, svars)) {
                    seq.Add(x);
                }
                return seq;
            }
            return TemplateEvaluator.Evaluate(staticVarCtx, "workflow-value", rawvars, 0, rawvars.FileId);

            static IEnumerable<KeyValuePair<ScalarToken, TemplateToken>> ProcessVariableMapping(IDictionary<string, VariableValue> vars, TemplateContext staticVarCtx, DictionaryContextData staticVars, MappingToken mvars)
            {
                for (int i = 0; i < mvars.Count; i++)
                {
                    var kv = mvars[i];
                    // Eval expressions if we parse static variables
                    if (staticVarCtx != null && (kv.Key is ExpressionToken || kv.Value is ExpressionToken))
                    {
                        // Need to group Expressions in keys as necessary
                        // Don't group independent if, elseif, else blocks
                        var evT = new MappingToken(kv.Key.FileId, kv.Key.Line, kv.Key.Column) { kv };
                        if (kv.Key.Type == TokenType.IfExpression)
                        {
                            while ((i + 1) < mvars.Count && (mvars[i + 1].Key.Type & (TokenType.ElseIfExpression | TokenType.ElseExpression)) != 0)
                            {
                                i++;
                                evT.Add(mvars[i]);
                                if (mvars[i].Key.Type == TokenType.ElseExpression)
                                {
                                    break;
                                }
                            }
                        }
                        var res = TemplateEvaluator.Evaluate(staticVarCtx, kv.Key is ExpressionToken ? "single-layer-workflow-mapping" : "workflow-value", evT, 0, kv.Key.FileId);
                        if (res is MappingToken r)
                        {
                            foreach(var l in ProcessVariableMapping(vars, staticVarCtx, staticVars, r)) {
                                yield return l;
                            }
                        }
                        continue;
                    }
                    var k = kv.Key.AssertString("variables").Value;
                    var v = kv.Value.AssertLiteralString("variables");
                    vars[k] = v;
                    if(staticVars != null) {
                        staticVars[k] = new StringContextData(v);
                    }
                    yield return kv;
                }
            }

            static async IAsyncEnumerable<TemplateToken> ProcessVariableSequence(Context context, IDictionary<string, VariableValue> vars, TemplateContext staticVarCtx, DictionaryContextData staticVars, SequenceToken svars)
            {
                for (int i = 0; i < svars.Count; i++)
                {
                    var rawdef = svars[i];
                    string template = null;
                    TemplateToken parameters = null;
                    string name = null;
                    string value = null;
                    bool isReadonly = false;
                    string group = null;
                    bool skip = false;
                    if (staticVars != null && rawdef is ExpressionToken)
                    {
                        var s = new SequenceToken(rawdef.FileId, rawdef.Line, rawdef.Column) { rawdef };
                        var res = TemplateEvaluator.Evaluate(staticVarCtx, "workflow-value", s, 0, rawdef.FileId);
                        if(res is SequenceToken st) {
                            await foreach(var t in ProcessVariableSequence(context, vars, staticVarCtx, staticVars, st)) {
                                yield return t;
                            }
                        }
                        continue;
                    }
                    var vdef = rawdef.AssertMapping("");
                    if (vdef.Count == 1 && vdef[0].Key.Type == TokenType.IfExpression && vdef[0].Value.Type == TokenType.Sequence)
                    {
                        var s = new SequenceToken(rawdef.FileId, rawdef.Line, rawdef.Column) { vdef };
                        while (i + 1 < svars.Count && svars[i + 1] is MappingToken mdef && mdef.Count == 1 && (mdef[0].Key.Type & (TokenType.ElseIfExpression | TokenType.ElseExpression)) != 0 && mdef[0].Value.Type == TokenType.Sequence)
                        {
                            i++;
                            s.Add(svars[i]);
                            if ((svars[i] as MappingToken)[0].Key.Type == TokenType.ElseExpression)
                            {
                                break;
                            }
                        }
                        var res = TemplateEvaluator.Evaluate(staticVarCtx, "single-layer-workflow-sequence", s, 0, rawdef.FileId);
                        if(res is SequenceToken st) {
                            await foreach(var t in ProcessVariableSequence(context, vars, staticVarCtx, staticVars, st)) {
                                yield return t;
                            }
                        }
                        continue;
                    }
                    foreach (var kv in vdef)
                    {
                        if (staticVars != null && (kv.Key is ExpressionToken || kv.Value is ExpressionToken))
                        {
                            skip = true;
                            break;
                        }
                        var primaryKey = kv.Key.AssertString("variables").Value;
                        switch (primaryKey)
                        {
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
                    if (skip)
                    {
                        if (skip)
                        {
                            var s = new SequenceToken(rawdef.FileId, rawdef.Line, rawdef.Column) { rawdef };
                            var res = TemplateEvaluator.Evaluate(staticVarCtx, "workflow-value", s, 0, rawdef.FileId);
                            if(res is SequenceToken st) {
                                await foreach(var t in ProcessVariableSequence(context, vars, staticVarCtx, staticVars, st)) {
                                    yield return t;
                                }
                            }
                        } else {
                            yield return rawdef;
                        }
                        continue;
                    }
                    if (group != null)
                    {
                        // Skip metainfo while preprocessing via staticVars != null
                        if (staticVars == null)
                        {
                            vars[Guid.NewGuid().ToString()] = new VariableValue(group) { IsGroup = true };
                        }
                        var groupVars = context.VariablesProvider?.GetVariablesForEnvironment(group);
                        if (groupVars != null)
                        {
                            foreach (var v in groupVars)
                            {
                                vars[v.Key] = new VariableValue(v.Value) { IsGroupMember = true };
                            }
                        }
                        yield return rawdef;
                    }
                    else if (template != null)
                    {
                        var evalp = parameters != null && staticVarCtx != null ? TemplateEvaluator.Evaluate(staticVarCtx, "workflow-value", parameters, 0, parameters.FileId) : null;
                        var file = await ReadTemplate(context, template, evalp != null ? evalp.AssertMapping("param").ToDictionary(kv => kv.Key.AssertString("").Value, kv => kv.Value) : null, "variable-template-root");
                        var res = await ParseVariables(context.ChildContext(file, template), vars, (from e in file where e.Key.AssertString("").Value == "variables" select e.Value).First(), staticVarCtx);
                        if (res is SequenceToken sq) {
                            await foreach(var x in ProcessVariableSequence(context, vars, staticVarCtx, staticVars, sq)) {
                                yield return x;
                            }
                        } else if (res is MappingToken mq) {
                            foreach(var x in ProcessVariableMapping(vars, staticVarCtx, staticVars, mq)) {
                                var mt = new MappingToken(x.Key.FileId, x.Key.Line, x.Key.Column)
                                {
                                    new KeyValuePair<ScalarToken, TemplateToken>(new StringToken(null, null, null, "name"), x.Key),
                                    new KeyValuePair<ScalarToken, TemplateToken>(new StringToken(null, null, null, "value"), x.Value)
                                };
                                yield return mt;
                            }
                        }
                    }
                    else
                    {
                        vars[name] = new VariableValue(value, isReadonly: isReadonly);
                        if(staticVars != null) {
                            staticVars[name] = new StringContextData(value);
                        }
                        yield return rawdef;
                    }
                }
            }
        }
        public static async Task ParseStep(Runner.Server.Azure.Devops.Context context, IList<TaskStep> steps, TemplateToken step) {
            var values = "task, powershell, pwsh, bash, script, checkout, download, downloadBuild, getPackage, publish, reviewApp, template";
            var mstep = step.AssertMapping($"steps.* a step must contain one of the following keyworlds as the first key {values}");
            mstep.AssertNotEmpty($"steps.* a step must contain one of the following keyworlds as the first key {values}");
            var tstep = new TaskStep();
            MappingToken unparsedTokens = new MappingToken(null, null, null);
            var primaryKey = mstep[0].Key.ToString();
            var isTemplate = primaryKey == "template";
            for(int i = 1; i < mstep.Count; i++) {
                if(isTemplate) {
                    unparsedTokens.Add(mstep[i]);
                    continue;
                }
                var key = mstep[i].Key.AssertString("step key").Value;
                var assertmessage = $"steps.*.{key}";
                switch(key) {
                    case "condition":
                        tstep.Condition = mstep[i].Value.AssertLiteralString(assertmessage);
                    break;
                    case "continueOnError":
                        tstep.ContinueOnError = new BooleanToken(null, null, null, mstep[i].Value.AssertAzurePipelinesBoolean(assertmessage));
                    break;
                    case "enabled":
                        tstep.Enabled = mstep[i].Value.AssertAzurePipelinesBoolean(assertmessage);
                    break;
                    case "retryCountOnTaskFailure":
                        tstep.RetryCountOnTaskFailure = mstep[i].Value.AssertAzurePipelinesInt32(assertmessage);
                    break;
                    case "timeoutInMinutes":
                        tstep.TimeoutInMinutes = new NumberToken(null, null, null, mstep[i].Value.AssertAzurePipelinesInt32(assertmessage));
                    break;
                    case "target":
                        if(mstep[i].Value is LiteralToken targetStr) {
                            tstep.Target = new StepTarget() { Target = targetStr.ToString() };
                        } else {
                            tstep.Target = new StepTarget();
                            foreach(var targetKv in mstep[i].Value.AssertMapping(assertmessage)) {
                                var targetKey = (targetKv.Key as StringToken).Value;
                                switch(targetKey) {
                                    case "container":
                                        tstep.Target.Target = targetKv.Value.AssertLiteralString($"{assertmessage}.{targetKey}");
                                    break;
                                    case "commands":
                                        tstep.Target.Commands = targetKv.Value.AssertLiteralString($"{assertmessage}.{targetKey}");
                                    break;
                                    case "settableVariables":
                                        tstep.Target.SettableVariables = new TaskVariableRestrictions();
                                        if((targetKv.Value as StringToken)?.Value != "none") {
                                            foreach(var svar in targetKv.Value.AssertSequence("SettableVariables sequence")) {
                                                tstep.Target.SettableVariables.Allowed.Add(svar.AssertLiteralString("SettableVariable"));
                                            }
                                        }
                                    break;
                                }
                            }
                        }
                    break;
                    case "env":
                        foreach(var kv in mstep[i].Value.AssertMapping(assertmessage)) {
                            var envKey = kv.Key.AssertString("env key").Value;
                            tstep.Environment[envKey] = kv.Value.AssertLiteralString($"{assertmessage}.{envKey}");
                        }
                    break;
                    case "name":
                        tstep.Name = mstep[i].Value.AssertLiteralString(assertmessage);
                    break;
                    case "displayName":
                        tstep.DisplayName = mstep[i].Value.AssertLiteralString(assertmessage);
                    break;
                    default:
                        unparsedTokens.Add(mstep[i]);
                    break;
                }
            }
            var primaryValue = mstep[0].Value.AssertLiteralString("step value");
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
                    if(unparsedTokens.Count == 1 && (unparsedTokens[0].Key as StringToken)?.Value != "parameters") {
                        throw new TemplateValidationException(new [] {new TemplateValidationError($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(mstep[2].Key)}Unexpected yaml key {(unparsedTokens[0].Key as StringToken)?.Value} expected parameters")});
                    }
                    if(mstep.Count > 2) {
                        throw new TemplateValidationException(new [] {new TemplateValidationError($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(mstep[2].Key)}Unexpected yaml keys {(mstep[2].Key as StringToken)?.Value} after template reference")});
                    }
                    try {
                        var file = await ReadTemplate(context, primaryValue, unparsedTokens.Count == 1 ? unparsedTokens[0].Value.AssertMapping("param").ToDictionary(kv => kv.Key.AssertString("").Value, kv => kv.Value) : null, "step-template-root");
                        await ParseSteps(context.ChildContext(file, primaryValue), steps, (from e in file where e.Key.AssertString("").Value == "steps" select e.Value).First().AssertSequence(""));
                    } catch(TemplateValidationException ex) {
                        throw new TemplateValidationException(ex.Errors.Prepend(new TemplateValidationError($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(mstep[0].Key)}Found Errors inside Template Reference: {ex.Message}")));
                    }
                break;
                default:
                throw new Exception($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(step)}Unexpected step.key got {primaryKey} expected {values}");
            }
        }

        public static async Task ParseSteps(Runner.Server.Azure.Devops.Context context, IList<TaskStep> steps, SequenceToken rawsteps) {
            var errors = new List<TemplateValidationError>();
            foreach(var step2 in rawsteps) {
                try {
                    await ParseStep(context, steps, step2);
                } catch(TemplateValidationException ex) {
                    errors.AddRange(ex.Errors);
                } catch(Exception ex) {
                    errors.Add(new TemplateValidationError($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(step2)}{ex.Message}"));
                }
            }
            if(errors.Any()) {
                throw new TemplateValidationException(errors);
            }
        }

        private static async Task<PipelineContextData> ConvertValue(Runner.Server.Azure.Devops.Context context, TemplateToken val, StringToken type, TemplateToken values) {
            var steps = new List<TaskStep>();
            var jobs = new List<Job>();
            var stages = new List<Stage>();
            Func<string, string> assertValues = s => {
                if(values != null) {
                    var validValues = new List<string>();
                    foreach(var aval in values.AssertSequence("parameters.*.values")) {
                        var validValue = aval.AssertLiteralString("parameters.*.values.*");
                        if(s == validValue) {
                            return s;
                        }
                        validValues.Add($"\"{validValue}\"");
                    }
                    throw new Exception($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(val)}\"{s}\" is not an allowed value expected {GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(values)}{String.Join(", ", validValues)} for this template value");
                }
                return s;
            };
            switch(type.Value) {
                case "object":
                // Now some unsupported types handle them as object
                case "environment":
                case "filePath":
                case "pool":
                case "secureFile":
                case "serviceConnection":
                return val == null ? null : val.ToContextData();
                case "legacyObject":
                return val == null ? null : ConvertAllScalarsToString(val).ToContextData();
                case "boolean":
                    if(val == null || string.Equals(val.AssertLiteralString("boolean"), "false", StringComparison.OrdinalIgnoreCase)) {
                        return new BooleanContextData(false);
                    }
                    if(string.Equals(val.AssertLiteralString("boolean"), "true", StringComparison.OrdinalIgnoreCase)) {
                        return new BooleanContextData(true);
                    }
                    throw new Exception($"{val.AssertLiteralString("boolean")} is not a valid boolean expected true or false (OrdinalIgnoreCase)");
                case "number":
                assertValues(val.AssertLiteralString("number"));
                return val == null ? new NumberContextData(0) : new NumberContextData(val.AssertAzurePipelinesDouble("number"));
                case "string":
                return val == null ? new StringContextData("") : new StringContextData(assertValues(val.AssertLiteralString("string")));
                case "step":
                if(val == null) {
                    return null;
                }
                await ParseStep(context, steps, val);
                return steps[0].ToContextData();
                case "stepList":
                if(val == null) {
                    return new ArrayContextData();
                }
                await ParseSteps(context, steps, val.AssertSequence(""));
                var stepList = new ArrayContextData();
                foreach(var step in steps) {
                    stepList.Add(step.ToContextData());
                }
                return stepList;
                case "job":
                if(val == null) {
                    return null;
                }
                return (await new Job().Parse(context, val)).ToContextData();
                case "jobList":
                if(val == null) {
                    return new ArrayContextData();
                }
                await Job.ParseJobs(context, jobs, val.AssertSequence(""));
                var jobList = new ArrayContextData();
                foreach(var job in jobs) {
                    jobList.Add(job.ToContextData());
                }
                return jobList;
                case "deployment":
                if(val == null) {
                    return null;
                }
                var djob = await new Job().Parse(context, val);
                return djob.ToContextData();
                case "deploymentList":
                if(val == null) {
                    return new ArrayContextData();
                }
                await Job.ParseJobs(context, jobs, val.AssertSequence(""));
                var djobList = new ArrayContextData();
                foreach(var job in jobs) {
                    djobList.Add(job.ToContextData());
                }
                return djobList;
                case "stage":
                if(val == null) {
                    return null;
                }
                return (await new Stage().Parse(context, val)).ToContextData();
                case "stageList":
                if(val == null) {
                    return new ArrayContextData();
                }
                await Stage.ParseStages(context, stages, val.AssertSequence(""));
                var stageList = new ArrayContextData();
                foreach(var stage in stages) {
                    stageList.Add(stage.ToContextData());
                }
                return stageList;
                case "container":
                if(val == null) {
                    return null;
                }
                return new Container().Parse(val.AssertMapping("")).ToContextData(val.AssertMapping("")[0].Value.AssertLiteralString(""));
                case "containerList":
                if(val == null) {
                    return new ArrayContextData();
                }
                var containerResources = new Dictionary<string, Container>(StringComparer.OrdinalIgnoreCase);
                foreach(var rawcontainer in val.AssertSequence("cres")) {
                    var container = rawcontainer.AssertMapping("");
                    containerResources[container[0].Value.AssertLiteralString("")] = new Container().Parse(container);
                }
                var containers = new ArrayContextData();
                foreach(var cr in containerResources) {
                    containers.Add(cr.Value.ToContextData(cr.Key));
                }
                return containers;
                default:
                throw new Exception($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(type)}This parameter type is not supported: " + type);
            }
        }

        public static string RelativeTo(string cwd, string filename) {
            var fullPath = $"{cwd}/{filename}";
            var seps = new char[] {'/', '\\'};
            if (filename.LastIndexOfAny(seps, 0) == 0) // use filename if absolute path is provided
            {
                fullPath = filename;
            }
            var path = fullPath.Split(seps).ToList();
            for(int i = 0; i < path.Count; i++) {
                if(path[i] == "." || string.IsNullOrEmpty(path[i])) {
                    path.RemoveAt(i);
                    i--;
                } else if(path[i] == ".." && i > 0 /* enshures i = i - 2 + 1 >= 0 */) {
                    path.RemoveAt(i); // Remove ..
                    path.RemoveAt(i - 1); // Remove the previous path component
                    i -= 2;
                }
            }
            return string.Join('/', path.ToArray());
        }

        private static ((int, int)[], Dictionary<(int, int), int>) CreateIdxMapping(TemplateToken token) {
            if(token is LiteralToken lit && lit.RawData != null) {
                var rand = new Random();
                string C = "CX";
                while(lit.RawData.Contains(C)) {
                    C = rand.Next(255).ToString("X2");
                }
                var praw = lit.ToString();
                (int, int)[] mapping = new (int, int)[praw.Length + 1];
                var rmapping = new Dictionary<(int, int), int>();
                Array.Fill(mapping, (-1, -1));

                int column = lit.Column.Value;
                int line = lit.Line.Value;
                int ridx = -1;
                for(int idx = 0; idx < lit.RawData.Length; idx++) {
                    if(lit.RawData[idx] == '\n') {
                        line++;
                        column = 1;
                        continue;
                    }
                    var xraw = lit.RawData.Insert(idx, C);

                    var scanner = new YamlDotNet.Core.Scanner(new StringReader(xraw), true);
                    try {
                        while(scanner.MoveNext() && !(scanner.Current is YamlDotNet.Core.Tokens.Error)) {
                            if(scanner.Current is YamlDotNet.Core.Tokens.Scalar s) {
                                var x = s.Value;
                                var m = x.IndexOf(C);
                                if(m >= 0 && m < mapping.Length && ridx <= m) {
                                    if(mapping[m] != (-1,-1)) {
                                        rmapping.Remove(mapping[m]);
                                    }
                                    mapping[m] = (line, column);
                                    rmapping[(line, column)] = m;
                                    ridx = m;
                                }
                            }
                        }
                    } catch {

                    }
                    column++;
                }
                return (mapping, rmapping);
            }
            return (null, null);
        }

        private static void SyntaxHighlightExpression(TemplateContext m_context, TemplateToken token, int offset, string value, int poffset = 0) {
            var (mapping, rmapping) = CreateIdxMapping(token);
            LexicalAnalyzer lexicalAnalyzer = new LexicalAnalyzer(value.Substring(offset), m_context.Flags);
            Token tkn = null;
            var startIndex = offset;
            var lit = token as LiteralToken;
            if(lit.RawData != null) {
                while(lexicalAnalyzer.TryGetNextToken(ref tkn)) {
                    var (r, c) = mapping[startIndex + tkn.Index];
                    if(tkn.Kind == TokenKind.Function) {
                        m_context.AddSemToken(r, c, tkn.RawValue.Length, 2, 2);
                    } else if(tkn.Kind == TokenKind.NamedValue) {
                        m_context.AddSemToken(r, c, tkn.RawValue.Length, 0, 1);
                    } else if(tkn.Kind == TokenKind.PropertyName) {
                        m_context.AddSemToken(r, c, tkn.RawValue.Length, 3, 0);
                    } else if(tkn.Kind == TokenKind.Boolean || tkn.Kind == TokenKind.Null) {
                        m_context.AddSemToken(r, c, tkn.RawValue.Length, 4, 2);
                    } else if(tkn.Kind == TokenKind.Number || tkn.Kind == TokenKind.String && tkn.ParsedValue is VersionWrapper) {
                        m_context.AddSemToken(r, c, tkn.RawValue.Length, 4, 4);
                    } else if(tkn.Kind == TokenKind.StartGroup || tkn.Kind == TokenKind.StartIndex || tkn.Kind == TokenKind.StartParameters || tkn.Kind == TokenKind.EndGroup || tkn.Kind == TokenKind.EndParameters
                        || tkn.Kind == TokenKind.EndIndex || tkn.Kind == TokenKind.Wildcard || tkn.Kind == TokenKind.Separator || tkn.Kind == TokenKind.LogicalOperator || tkn.Kind == TokenKind.Dereference) {
                        m_context.AddSemToken(r, c, tkn.RawValue.Length, 5, 0);
                    } else if(tkn.Kind == TokenKind.String) {
                        var (er, ec) = mapping[startIndex + tkn.Index + tkn.RawValue.Length];
                        // Only add single line string
                        if(er == r && c < ec) {
                            m_context.AddSemToken(r, c, ec - c /* May contain escape codes */, 6, 0);
                        }
                        // TODO multi line string by splitting them
                    }
                }
            }
        }

        private static bool AutoCompleteExpression(TemplateContext m_context, AutoCompleteEntry completion, int offset, string value, int poffset = 0) {
            if(completion != null && m_context.AutoCompleteMatches != null) {
                var idx = GetIdxOfExpression(completion.Token as LiteralToken, m_context.Row.Value, m_context.Column.Value);
                var startIndex = -1 - completion.Index + offset;
                if(idx == -1 && completion.Token.PreWhiteSpace != null && (m_context.Row.Value > completion.Token.PreWhiteSpace.Line || m_context.Row.Value == completion.Token.PreWhiteSpace.Line && m_context.Column.Value >= completion.Token.PostWhiteSpace.Character)) {
                    idx = startIndex;
                }
                if(idx == -1 && completion.Token.PostWhiteSpace != null && (m_context.Row.Value < completion.Token.PostWhiteSpace.Line || m_context.Row.Value == completion.Token.PostWhiteSpace.Line && m_context.Column.Value <= completion.Token.PostWhiteSpace.Character)) {
                    idx = startIndex + value.Length + poffset;
                }
                if(idx != -1 && idx >= startIndex && (idx <= startIndex + value.Length + poffset)) {
                    LexicalAnalyzer lexicalAnalyzer = new LexicalAnalyzer(value, m_context.Flags);
                    Token tkn = null;
                    List<Token> tkns = new List<Token>();
                    while(lexicalAnalyzer.TryGetNextToken(ref tkn)) {
                        tkns.Add(tkn);
                        if(tkn.Index + startIndex > idx) {
                            break;
                        }
                    }
                    completion.Tokens = tkns;
                    completion.Index = idx - startIndex;
                }
            }
            return true;
        }

        private static int GetIdxOfExpression(LiteralToken lit, int row, int column)
        {
          var lc = column - lit.Column;
          var lr = row - lit.Line;
          var rand = new Random();
          string C = "CX";
          while(lit.RawData.Contains(C)) {
            C = rand.Next(255).ToString("X2");
          }
          var xraw = lit.RawData;
          var idx = 0;
          for(int i = 0; i < lr; i++) {
            var n = xraw.IndexOf('\n', idx);
            if(n == -1) {
              return -1;
            }
            idx = n + 1;
          }
          idx += idx == 0 ? lc ?? 0 : column - 1;
          if(idx < 0 || idx > xraw.Length) {
            return -1;
          }
          xraw = xraw.Insert(idx, C);

          var scanner = new YamlDotNet.Core.Scanner(new StringReader(xraw), true);
          try {
            while(scanner.MoveNext() && !(scanner.Current is YamlDotNet.Core.Tokens.Error)) {
              if(scanner.Current is YamlDotNet.Core.Tokens.Scalar s) {
                var x = s.Value;
                return x.IndexOf(C);
              }
            }
          } catch {

          }
          return -1;
        }

        
        public static async Task<(string, TemplateToken)> ParseTemplate(Context context, string filenameAndRef, string schemaName = null, bool checks = true)
        {
            var afilenameAndRef = filenameAndRef.Split("@", 2);
            var filename = afilenameAndRef[0];
            // Read the file
            var finalRepository = afilenameAndRef.Length == 1 ? context.RepositoryAndRef : string.Equals(afilenameAndRef[1], "self", StringComparison.OrdinalIgnoreCase) ? null : (context.Repositories?.TryGetValue(afilenameAndRef[1], out var ralias) ?? false) ? ralias : throw new Exception($"Couldn't find repository with alias {afilenameAndRef[1]} in repository resources");
            var finalFileName = RelativeTo(context.RepositoryAndRef == finalRepository ? context.CWD ?? "." : "/", filename);
            if (finalFileName == null)
            {
                throw new Exception($"Couldn't find template location {filenameAndRef}");
            }

            var fileContent = await context.FileProvider.ReadFile(finalRepository, finalFileName);
            if (fileContent == null)
            {
                throw new Exception($"Couldn't read template {filenameAndRef} resolved to {finalFileName} ({finalRepository ?? "self"})");
            }
            context.TraceWriter?.Info("{0}", $"Parsing template {filenameAndRef} resolved to {finalFileName} ({finalRepository ?? "self"}) using Schema {schemaName ?? "pipeline-root"}");
            context.TraceWriter?.Verbose("{0}", fileContent);

            var errorTemplateFileName = $"({finalRepository ?? "self"})/{finalFileName}";
            context.FileTable ??= new List<string>();
            context.FileTable.Add(errorTemplateFileName);
            var templateContext = AzureDevops.CreateTemplateContext(context.TraceWriter ?? new EmptyTraceWriter(), context.FileTable, context.Flags);
            if(context.Column != 0 && context.Row != 0) {
                templateContext.AutoCompleteMatches = context.AutoCompleteMatches ??= new List<AutoCompleteEntry>();
                templateContext.Column = context.Column;
                templateContext.Row = context.Row;
            }
            var fileId = templateContext.GetFileId(errorTemplateFileName);

            // preserveString is needed for azure pipelines compatibility of the templateContext property all boolean and number token are casted to string without loosing it's exact string value
            var yamlObjectReader = new YamlObjectReader(fileId, fileContent, preserveString: true, forceAzurePipelines: true, rawMapping: context.RawMapping);
            TemplateToken token = TemplateReader.Read(templateContext, schemaName ?? "pipeline-root", yamlObjectReader, fileId, out _);

            context.SemTokens = templateContext.SemTokens;
            if(checks)
            {
                foreach (var stepCond in token.TraverseByPattern(new[] { "steps", "*", "condition" })
                                    .Concat(token.TraverseByPattern(new[] { "jobs", "*", "steps", "*", "condition" }))
                                    .Concat(token.TraverseByPattern(new[] { "stages", "*", "jobs", "*", "steps", "*", "condition" }))
                                    .Concat(token.TraverseByPattern(new[] { "jobs", "*", "strategy", "", "steps", "*", "condition" }))
                                    .Concat(token.TraverseByPattern(new[] { "jobs", "*", "strategy", "on", "", "steps", "*", "condition" }))
                                    .Concat(token.TraverseByPattern(new[] { "stages", "*", "jobs", "*", "strategy", "", "steps", "*", "condition" }))
                                    .Concat(token.TraverseByPattern(new[] { "stages", "*", "jobs", "*", "strategy", "on", "", "steps", "*", "condition" }))
                    )
                {
                    CheckConditionalExpressions(templateContext, stepCond, Level.Step);
                }
                foreach (var jobCond in token.TraverseByPattern(new[] { "jobs", "*", "condition" })
                                    .Concat(token.TraverseByPattern(new[] { "stages", "*", "jobs", "*", "condition" }))
                    )
                {
                    CheckConditionalExpressions(templateContext, jobCond, Level.Job);
                }
                foreach (var stageCond in token.TraverseByPattern(new[] { "stages", "*", "condition" })
                    )
                {
                    CheckConditionalExpressions(templateContext, stageCond, Level.Stage);
                }
                foreach (var runtimeExpr in token.TraverseByPattern(new[] { "variables", "*", "value" })
                                    .Concat(token.TraverseByPattern(new[] { "variables", "" }))
                                    .Concat(token.TraverseByPattern(new[] { "continueOnError" }))
                                    .Concat(token.TraverseByPattern(new[] { "container" }))
                                    .Concat(token.TraverseByPattern(new[] { "container", "alias" }))
                                    .Concat(token.TraverseByPattern(new[] { "container", "image" }))
                                    .Concat(token.TraverseByPattern(new[] { "strategy", "matrix" }))
                                    .Concat(token.TraverseByPattern(new[] { "strategy", "maxParallel" }))
                                    .Concat(token.TraverseByPattern(new[] { "strategy", "parallel" }))
                                    .Concat(token.TraverseByPattern(new[] { "stages", "*", "variables", "*", "value" }))
                                    .Concat(token.TraverseByPattern(new[] { "stages", "*", "variables", "" }))
                                    .Concat(token.TraverseByPattern(new[] { "stages", "*", "jobs", "*", "variables", "*", "value" }))
                                    .Concat(token.TraverseByPattern(new[] { "stages", "*", "jobs", "*", "variables", "" }))
                                    .Concat(token.TraverseByPattern(new[] { "stages", "*", "jobs", "*", "continueOnError" }))
                                    .Concat(token.TraverseByPattern(new[] { "stages", "*", "jobs", "*", "timeoutInMinutes" }))
                                    .Concat(token.TraverseByPattern(new[] { "stages", "*", "jobs", "*", "cancelTimeoutInMinutes" }))
                                    .Concat(token.TraverseByPattern(new[] { "stages", "*", "jobs", "*", "container" }))
                                    .Concat(token.TraverseByPattern(new[] { "stages", "*", "jobs", "*", "container", "alias" }))
                                    .Concat(token.TraverseByPattern(new[] { "stages", "*", "jobs", "*", "container", "image" }))
                                    .Concat(token.TraverseByPattern(new[] { "stages", "*", "jobs", "*", "strategy", "matrix" }))
                                    .Concat(token.TraverseByPattern(new[] { "stages", "*", "jobs", "*", "strategy", "maxParallel" }))
                                    .Concat(token.TraverseByPattern(new[] { "stages", "*", "jobs", "*", "strategy", "parallel" }))
                                    .Concat(token.TraverseByPattern(new[] { "jobs", "*", "variables", "*", "value" }))
                                    .Concat(token.TraverseByPattern(new[] { "jobs", "*", "variables", "" }))
                                    .Concat(token.TraverseByPattern(new[] { "jobs", "*", "continueOnError" }))
                                    .Concat(token.TraverseByPattern(new[] { "jobs", "*", "timeoutInMinutes" }))
                                    .Concat(token.TraverseByPattern(new[] { "jobs", "*", "cancelTimeoutInMinutes" }))
                                    .Concat(token.TraverseByPattern(new[] { "jobs", "*", "container" }))
                                    .Concat(token.TraverseByPattern(new[] { "jobs", "*", "container", "alias" }))
                                    .Concat(token.TraverseByPattern(new[] { "jobs", "*", "container", "image" }))
                                    .Concat(token.TraverseByPattern(new[] { "jobs", "*", "strategy", "matrix" }))
                                    .Concat(token.TraverseByPattern(new[] { "jobs", "*", "strategy", "maxParallel" }))
                                    .Concat(token.TraverseByPattern(new[] { "jobs", "*", "strategy", "parallel" }))
                    )
                {
                    CheckSingleRuntimeExpression(templateContext, runtimeExpr);
                }

                try
                {
                    var cCtx = context.ChildContext(token as MappingToken, finalFileName);
                    await processParams(templateContext, fileId, cCtx, token, "");
                    foreach (var (extends, schema) in token.TraverseByPattern(new[] { "extends" }).Select(e => (e, "extend-template-root"))
                        .Concat(token.TraverseByPattern(new[] { "variables", "*" }).Select(e => (e, "variable-template-root")))
                        .Concat(token.TraverseByPattern(new[] { "stages", "*" }).Select(e => (e, "stage-template-root")))
                        .Concat(token.TraverseByPattern(new[] { "jobs", "*" }).Select(e => (e, "job-template-root")))
                        .Concat(token.TraverseByPattern(new[] { "stages", "*", "jobs", "*" }).Select(e => (e, "job-template-root")))
                        .Concat(token.TraverseByPattern(new[] { "stages", "*", "variables", "*" }).Select(e => (e, "variable-template-root")))
                        .Concat(token.TraverseByPattern(new[] { "steps", "*" }).Select(e => (e, "step-template-root")))
                        .Concat(token.TraverseByPattern(new[] { "jobs", "*", "variables", "*" }).Select(e => (e, "variable-template-root")))
                        .Concat(token.TraverseByPattern(new[] { "jobs", "*", "steps", "*" }).Select(e => (e, "step-template-root")))
                        .Concat(token.TraverseByPattern(new[] { "jobs", "*", "strategy", "", "steps", "*" }).Select(e => (e, "step-template-root")))
                        .Concat(token.TraverseByPattern(new[] { "jobs", "*", "strategy", "on", "", "steps", "*" }).Select(e => (e, "step-template-root")))
                        .Concat(token.TraverseByPattern(new[] { "stages", "*", "jobs", "*", "variables", "*" }).Select(e => (e, "variable-template-root")))
                        .Concat(token.TraverseByPattern(new[] { "stages", "*", "jobs", "*", "steps", "*" }).Select(e => (e, "step-template-root")))
                        .Concat(token.TraverseByPattern(new[] { "stages", "*", "jobs", "*", "strategy", "", "steps", "*" }).Select(e => (e, "step-template-root")))
                        .Concat(token.TraverseByPattern(new[] { "stages", "*", "jobs", "*", "strategy", "on", "", "steps", "*" }).Select(e => (e, "step-template-root")))
                    )
                    {
                        await processTemplates(templateContext, fileId, cCtx, extends, schema);
                    }
                }
                catch
                {

                }
            }

            templateContext.Errors.Check();

            return (errorTemplateFileName, token);

            static async Task processTemplates(TemplateContext templateContext, int fileId, Context cCtx, TemplateToken extends, string schema)
            {
                var template = extends.TraverseByPattern(new[] { "template" }).FirstOrDefault();
                var parameters = extends.TraverseByPattern(new[] { "parameters" }).FirstOrDefault() as MappingToken;

                if (template is StringToken stringToken && parameters != null)
                {
                    var (name, subtmpl) = await ParseTemplate(cCtx, stringToken.ToString(), schema, false);
                    if (subtmpl.TraverseByPattern(new[] { "parameters" }).FirstOrDefault() is SequenceToken)
                    {
                        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var paramdef in subtmpl.TraverseByPattern(new[] { "parameters", "*" }))
                        {
                            var namedef = paramdef.TraverseByPattern(new[] { "name" }).FirstOrDefault()?.ToString();
                            var typedef = paramdef.TraverseByPattern(new[] { "type" }).FirstOrDefault()?.ToString() ?? "string";
                            dict[namedef] = typedef;
                            var val = parameters.TraverseByPattern(new[] { namedef }).FirstOrDefault();
                            string fdef = null;
                            int? start = null;
                            switch (typedef)
                            {
                                case "stageList":
                                    fdef = "stages";
                                    start = 1;
                                    break;
                                case "step":
                                    fdef = "step";
                                    start = 6;
                                    break;
                                case "stage":
                                    fdef = "stage";
                                    start = 2;
                                    break;
                                case "job":
                                case "deployment":
                                    fdef = "job";
                                    start = 4;
                                    break;
                                case "jobList":
                                case "deploymentList":
                                    fdef = "jobs";
                                    start = 3;
                                    break;
                                case "stepList":
                                    fdef = "steps";
                                    start = 5;
                                    break;
                                case "container":
                                    fdef = "containerResource";
                                    break;
                                case "containerList":
                                    fdef = "containerResources";
                                    break;
                                case "number":
                                    fdef = "string";
                                    break;
                                case "boolean":
                                    fdef = "string";
                                    break;
                                case "string":
                                    fdef = "string";
                                    break;
                            }
                            if (fdef != null && val != null)
                            {
                                // var autoCompleteState = templateContext.AutoCompleteMatches;
                                // templateContext.AutoCompleteMatches = new List<AutoCompleteEntry>();
                                TemplateEvaluator.Evaluate(templateContext, fdef, val, 0, fileId);
                                if(start != null) {
                                    foreach(var (tkn, sh) in GetPatterns(start.Value)
                                        .SelectMany(v => val.TraverseByPattern(v.Item1).Select(t => (t, v.Item2))))
                                    {
                                        switch(sh) {
                                            case 0:
                                                CheckConditionalExpressions(templateContext, tkn, Level.Step);
                                            break;
                                            case 1:
                                                CheckConditionalExpressions(templateContext, tkn, Level.Job);
                                            break;
                                            case 2:
                                                CheckConditionalExpressions(templateContext, tkn, Level.Stage);
                                            break;
                                            case 3:
                                                CheckSingleRuntimeExpression(templateContext, tkn);
                                            break;
                                        }
                                    }

                                    Func<int, IEnumerable<(TemplateToken, string)>> jobDeps = (s) => {
                                        var ret = Array.Empty<(TemplateToken, string)>().AsEnumerable();
                                        if(s > 6) {
                                            return ret;
                                        }
                                        for(int t = 6; t > s; t -= 2) {
                                            string sch = null;
                                            switch(t) {
                                                case 6:
                                                    sch = "step-template-root";
                                                    break;
                                                case 4:
                                                    sch = "job-template-root";
                                                    break;
                                                case 2:
                                                    sch = "stage-template-root";
                                                    break;
                                            }
                                            if(sch == null) {
                                                continue;
                                            }
                                            ret = ret
                                                .Concat(val.TraverseByPattern(new [] { "stages", "*", "jobs", "*", "steps", "*" }.Take(t).Skip(s).ToArray()).Select(e => (e, sch)));
                                            if(s <= 4 && (t - 2) >= s) {
                                                ret = ret
                                                    .Concat(val.TraverseByPattern(new [] { "stages", "*", "jobs", "*" }.Take(t - 2).Skip(s).Concat(new [] { "variables", "*" }).ToArray()).Select(e => (e, "variable-template-root")));
                                            }
                                        }
                                        if(s > 4) {
                                            return ret;
                                        }
                                        return ret
                                            .Concat(val.TraverseByPattern(new [] { "stages", "*", "jobs", "*", "strategy", "", "steps", "*" }.Skip(s).ToArray()).Select(e => (e, "step-template-root")))
                                            .Concat(val.TraverseByPattern(new [] { "stages", "*", "jobs", "*", "strategy", "on", "", "steps", "*" }.Skip(s).ToArray()).Select(e => (e, "step-template-root")));
                                    };

                                    foreach(var (tkn, sh) in jobDeps(start.Value))
                                    {
                                        await processTemplates(templateContext, fileId, cCtx, tkn, sh);
                                    }
                                }
                            }
                        }

                        for (int i = 0; i < parameters.Count; i++)
                        {
                            if (!(parameters[i].Key is ExpressionToken))
                            {
                                var skey = parameters[i].Key.ToString();
                                if (!dict.ContainsKey(skey))
                                {
                                    templateContext.Errors.Add($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(parameters[i].Key)}Unexpected parameter '{skey}'");
                                }
                            }
                        }
                    }
                }
            }

            static async Task processParams(TemplateContext templateContext, int fileId, Context cCtx, TemplateToken subtmpl, string schema)
            {

                if (subtmpl.TraverseByPattern(new[] { "parameters" }).FirstOrDefault() is SequenceToken)
                {
                    foreach (var paramdef in subtmpl.TraverseByPattern(new[] { "parameters", "*" }))
                    {
                        var namedef = paramdef.TraverseByPattern(new[] { "name" }).FirstOrDefault()?.ToString();
                        var typedef = paramdef.TraverseByPattern(new[] { "type" }).FirstOrDefault()?.ToString() ?? "string";
                        var val = paramdef.TraverseByPattern(new[] { "default" }).FirstOrDefault();
                        string fdef = null;
                        int? start = null;
                        switch (typedef)
                        {
                            case "stageList":
                                fdef = "stages";
                                start = 1;
                                break;
                            case "step":
                                fdef = "step";
                                start = 6;
                                break;
                            case "stage":
                                fdef = "stage";
                                start = 2;
                                break;
                            case "job":
                            case "deployment":
                                fdef = "job";
                                start = 4;
                                break;
                            case "jobList":
                            case "deploymentList":
                                fdef = "jobs";
                                start = 3;
                                break;
                            case "stepList":
                                fdef = "steps";
                                start = 5;
                                break;
                            case "container":
                                fdef = "containerResource";
                                break;
                            case "containerList":
                                fdef = "containerResources";
                                break;
                            case "number":
                                fdef = "string";
                                break;
                            case "boolean":
                                fdef = "string";
                                break;
                            case "string":
                                fdef = "string";
                                break;
                        }
                        if (fdef != null && val != null)
                        {
                            TemplateEvaluator.Evaluate(templateContext, fdef, val, 0, fileId);
                            if(start != null) {
                                foreach(var (tkn, sh) in GetPatterns(start.Value)
                                    .SelectMany(v => val.TraverseByPattern(v.Item1).Select(t => (t, v.Item2))))
                                {
                                    switch(sh) {
                                        case 0:
                                            CheckConditionalExpressions(templateContext, tkn, Level.Step);
                                        break;
                                        case 1:
                                            CheckConditionalExpressions(templateContext, tkn, Level.Job);
                                        break;
                                        case 2:
                                            CheckConditionalExpressions(templateContext, tkn, Level.Stage);
                                        break;
                                        case 3:
                                            CheckSingleRuntimeExpression(templateContext, tkn);
                                        break;
                                    }
                                }

                                Func<int, IEnumerable<(TemplateToken, string)>> jobDeps = (s) => {
                                    var ret = Array.Empty<(TemplateToken, string)>().AsEnumerable();
                                    if(s > 6) {
                                        return ret;
                                    }
                                    for(int t = 6; t > s; t -= 2) {
                                        string sch = null;
                                        switch(t) {
                                            case 6:
                                                sch = "step-template-root";
                                                break;
                                            case 4:
                                                sch = "job-template-root";
                                                break;
                                            case 2:
                                                sch = "stage-template-root";
                                                break;
                                        }
                                        if(sch == null) {
                                            continue;
                                        }
                                        ret = ret
                                            .Concat(val.TraverseByPattern(new [] { "stages", "*", "jobs", "*", "steps", "*" }.Take(t).Skip(s).ToArray()).Select(e => (e, sch)));                                        }
                                    if(s > 4) {
                                        return ret;
                                    }
                                    return ret
                                        .Concat(val.TraverseByPattern(new [] { "stages", "*", "jobs", "*", "strategy", "", "steps", "*" }.Skip(s).ToArray()).Select(e => (e, "step-template-root")))
                                        .Concat(val.TraverseByPattern(new [] { "stages", "*", "jobs", "*", "strategy", "on", "", "steps", "*" }.Skip(s).ToArray()).Select(e => (e, "step-template-root")));
                                };

                                foreach(var (tkn, sh) in jobDeps(start.Value))
                                {
                                    await processTemplates(templateContext, fileId, cCtx, tkn, sh);
                                }
                            }
                        }
                    }
                }
            }

            static IEnumerable<(string[], int)> GetPatterns(int j)
            {
                var condition = new [] { "condition" };

                var stepPattern = new [] { "steps", "*" };
                var stepJobPattern = new string[][] {
                    new [] { "strategy", "", "steps", "*" },
                    new [] { "strategy", "on", "", "steps", "*" },
                }.Append(stepPattern).ToArray();
                var jobPattern = new [] { "jobs", "*" };
                var stepStagePattern = stepJobPattern.SelectMany(p => jobPattern.Concat(p)).ToArray();
                var stagePattern = new [] { "stages", "*" };
                var stepWorkflowPattern = stepStagePattern.SelectMany(p => stagePattern.Select(j => j.Concat(p))).ToArray();
                var jobWorkflowPattern = jobPattern.SelectMany(p => stagePattern.Append(p)).ToArray();

                var jobRuntimeExpr = new string[][] {
                    new[] { "continueOnError" },
                    new[] { "timeoutInMinutes" },
                    new[] { "cancelTimeoutInMinutes" },
                    new[] { "container" },
                    new[] { "container", "alias" },
                    new[] { "container", "image" },
                    new[] { "strategy", "matrix" },
                    new[] { "strategy", "maxParallel" },
                    new[] { "strategy", "parallel" },
                };

                var varRuntimeExpr = new string[][] {
                    new[] { "variables", "*", "value" },
                    new[] { "variables", "" },
                };

                var star = new [] { "*" };
                var stepType = new [] { (condition, 0) };
                if (j == 6)
                {
                    return stepType;
                }
                var stepsType = stepType.Select(v => (star.Concat(v.Item1).ToArray(), v.Item2));
                if (j == 5)
                {
                    return stepsType.ToArray();
                }
                var jobType = stepJobPattern.Select(s => (s.Concat(condition).ToArray(), 0)).Append((condition, 1)).Concat(varRuntimeExpr.Concat(jobRuntimeExpr).Select(v => (v, 3)));
                if (j == 4)
                {
                    return jobType.ToArray();
                }
                var jobsType = jobType.Select(s => (star.Concat(s.Item1).ToArray(), s.Item2));
                if (j == 3)
                {
                    return jobsType.ToArray();
                }
                var stageType = jobType.Select(s => (jobPattern.Concat(s.Item1).ToArray(), s.Item2)).Append((condition, 2)).Concat(varRuntimeExpr.Select(v => (v, 3)));
                if (j == 2)
                {
                    return stageType.ToArray();
                }
                var stagesType = stageType.Select(s => (star.Concat(s.Item1).ToArray(), s.Item2));
                if (j == 1)
                {
                    return stagesType.ToArray();
                }
                return jobType.Concat(stageType).Where(s => !s.Item1.SequenceEqual(condition)).Concat(stageType.Select(s => (stagePattern.Concat(s.Item1).ToArray(), s.Item2))).Concat(varRuntimeExpr.Select(v => (v, 3)));
            }

        }

        private static void CheckSingleRuntimeExpression(TemplateContext m_context, TemplateToken rawVal)
        {
            var autoComplete = m_context.AutoCompleteMatches?.Count > 0 && m_context.AutoCompleteMatches.Last().Token == rawVal;
            if(rawVal == null || !autoComplete && rawVal.Type == TokenType.Null || !(rawVal is LiteralToken) || rawVal.Type == TokenType.BasicExpression) {
                return;
            }
            var val = rawVal.AssertLiteralString("runtime expression");
            if (val == null || !(val.StartsWith("$[") && val.EndsWith("]")))
            {
                return;
            }
            try {
                var pval = val.Substring(2, val.Length - 3);
                var names = new[] { "variables", "resources", "pipeline", "dependencies", "stageDependencies" };
                if(autoComplete) {
                    var match = m_context.AutoCompleteMatches.Last();
                    match.AllowedContext = names.Append("counter(0,2)").ToArray();
                    AutoCompleteExpression(m_context, match, 2, pval);
                }
                // m_context.AddSemToken(rawVal.Line.Value, rawVal.Column.Value, 2, 5, 0);
                SyntaxHighlightExpression(m_context, rawVal, 2, val);
                // m_context.AddSemToken(rawVal.Line.Value, rawVal.Column.Value, 2, 5, 0);
                var parser = new ExpressionParser() { Flags = ExpressionFlags.DTExpressionsV1 | ExpressionFlags.ExtendedDirectives | ExpressionFlags.AllowAnyForInsert };
                var node = parser.CreateTree(pval, new EmptyTraceWriter().ToExpressionTraceWriter(),
                    names.Select(n => new NamedValueInfo<NoOperationNamedValue>(n)),
                    ExpressionConstants.AzureWellKnownFunctions.Where(kv => kv.Key != "split").Select(kv => kv.Value).Append(new FunctionInfo<NoOperation>("counter", 0, 2)));
            } catch (Exception ex) {
                m_context.Errors.Add($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(rawVal)}{ex.Message}");
            }
        }

        private enum Level {
            Stage,
            Job,
            Step
        }

        private static void CheckConditionalExpressions(TemplateContext m_context, TemplateToken rawCondition, Level level)
        {
            var autoComplete = m_context.AutoCompleteMatches?.Count > 0 && m_context.AutoCompleteMatches.Last().Token == rawCondition;
            if(rawCondition == null || !autoComplete && rawCondition.Type == TokenType.Null || !(rawCondition is LiteralToken) || rawCondition.Type == TokenType.BasicExpression) {
                return;
            }
            var val = rawCondition.AssertLiteralString("condition");
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
                if(autoComplete) {
                    var match = m_context.AutoCompleteMatches.Last();
                    match.AllowedContext = names.Concat(funcs.Select(f => $"{f.Name}({f.MinParameters},{f.MaxParameters})")).ToArray();
                    if(rawCondition.Type == TokenType.Null) {
                        match.Token = new NullToken(match.Token.FileId, match.Token.Line, match.Token.Column + 1, "");
                    }
                    AutoCompleteExpression(m_context, match, rawCondition.Type == TokenType.Null ? 1 : 0, val);
                }
                SyntaxHighlightExpression(m_context, rawCondition, 0, val);
                
                var parser = new ExpressionParser() { Flags = ExpressionFlags.DTExpressionsV1 | ExpressionFlags.ExtendedDirectives | ExpressionFlags.AllowAnyForInsert };
                var node = parser.CreateTree(val, new EmptyTraceWriter().ToExpressionTraceWriter(),
                    names.Select(n => new NamedValueInfo<NoOperationNamedValue>(n)),
                    ExpressionConstants.AzureWellKnownFunctions.Where(kv => kv.Key != "split").Select(kv => kv.Value).Concat(funcs));
            } catch (Exception ex) {
                m_context.Errors.Add($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(rawCondition)}{ex.Message}");
            }
        }
        
        public static async Task<MappingToken> ReadTemplate(Runner.Server.Azure.Devops.Context context, string filenameAndRef, Dictionary<string, TemplateToken> cparameters = null, string schemaName = null)
        {
            var variables = context.VariablesProvider?.GetVariablesForEnvironment("");
            var (errorTemplateFileName, token) = await ParseTemplate(context, filenameAndRef, schemaName);

            var pipelineroot = token.AssertMapping("root");

            var childContext = context.ChildContext(pipelineroot, filenameAndRef);

            TemplateToken parameters = null;
            TemplateToken rawStaticVariables = null;
            foreach (var kv in pipelineroot)
            {
                if (kv.Key.Type != TokenType.String)
                {
                    continue;
                }
                switch ((kv.Key as StringToken)?.Value)
                {
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
            if (variables != null)
            {
                foreach (var v in variables)
                {
                    variablesData[v.Key] = new StringContextData(v.Value);
                }
            }


            var templateContext = AzureDevops.CreateTemplateContext(context.TraceWriter ?? new EmptyTraceWriter(), context.FileTable, context.Flags, contextData);
            var fileId = templateContext.GetFileId(errorTemplateFileName);

            var strictParametersCheck = false;
            if (parameters?.Type == TokenType.Mapping)
            {
                foreach (var kv in parameters as MappingToken)
                {
                    if (kv.Key.Type != TokenType.String)
                    {
                        continue;
                    }
                    var paramname = (kv.Key as StringToken)?.Value;
                    if (cparameters?.TryGetValue(paramname, out var value) == true)
                    {
                        parametersData[paramname] = ConvertAllScalarsToString(value).ToContextData();
                    }
                    else
                    {
                        parametersData[paramname] = ConvertAllScalarsToString(kv.Value).ToContextData();
                    }
                }
            }
            else if (parameters is SequenceToken sparameters)
            {
                // Only new style templates should provide errors
                // old style templates only warn if implemented
                strictParametersCheck = true;
                foreach (var mparam in sparameters)
                {
                    var varm = mparam.AssertMapping("varm");
                    string name = null;
                    StringToken type = new StringToken(null, null, null, "string");
                    TemplateToken def = null;
                    TemplateToken values = null;
                    foreach (var kv in varm)
                    {
                        switch ((kv.Key as StringToken).Value)
                        {
                            case "name":
                                name = kv.Value.AssertLiteralString("name");
                                break;
                            case "type":
                                type = new StringToken(kv.Value.FileId, kv.Value.Line, kv.Value.Column, kv.Value.AssertLiteralString("type"));
                                break;
                            case "default":
                                def = kv.Value;
                                break;
                            case "values":
                                values = kv.Value;
                                break;
                        }
                    }
                    if (name == null)
                    {
                        templateContext.Error(varm, "A value for the 'name' parameter must be provided.");
                        continue;
                    }
                    var defCtxData = def == null ? null : await ConvertValue(context, def, type, values);
                    if (cparameters?.TryGetValue(name, out var value) == true || def == null && (value = await (context.RequiredParametersProvider?.GetRequiredParameter(name) ?? Task.FromResult<TemplateToken>(null))) != null)
                    {
                        parametersData[name] = await ConvertValue(context, value, type, values);
                    }
                    else
                    {
                        if (def == null) // handle missing required parameter
                        {
                            templateContext.Error(mparam, $"A value for the '{name}' parameter must be provided.");
                        }
                        parametersData[name] = defCtxData;
                    }
                }

                if (cparameters != null)
                {
                    foreach (var unexpectedParameter in cparameters.Where(kv => !parametersData.ContainsKey(kv.Key)))
                    {
                        templateContext.Error(unexpectedParameter.Value ?? parameters, $"Unexpected parameter '{unexpectedParameter.Key}'");
                    }
                }
            }

            templateContext.Errors.Check();
            if(!strictParametersCheck && cparameters != null) {
                foreach(var param in cparameters) {
                    parametersData[param.Key] = ConvertAllScalarsToString(param.Value).ToContextData();
                }
            }

            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var param in parametersData)
            {
                dict[param.Key] = param.Value;
            }

            if (rawStaticVariables != null)
            {
                // See "testworkflows/azpipelines/expressions-docs/Conditionally assign a variable.yml"
                templateContext = AzureDevops.CreateTemplateContext(context.TraceWriter ?? new EmptyTraceWriter(), templateContext.GetFileTable().ToArray(), context.Flags, contextData);
                if(strictParametersCheck) {
                    templateContext.ExpressionValues["parameters"] = new ParametersContextData(dict, templateContext.Errors);
                }
                // rawStaticVariables = TemplateEvaluator.Evaluate(templateContext, "workflow-value", rawStaticVariables, 0, fileId);
                // templateContext.Errors.Check();

                IDictionary<string, VariableValue> pvars = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
                pipelineroot[pipelineroot.Select((x, i) => (x, i)).First(x => x.x.Key.ToString() == "variables").i] = new KeyValuePair<ScalarToken, TemplateToken>(new StringToken(null, null, null, "variables"), await ParseVariables(childContext, pvars, rawStaticVariables, templateContext));
                foreach (var v in pvars)
                {
                    variablesData[v.Key] = new StringContextData(v.Value.Value);
                }
            }

            //pipelineroot.Traverse().Any(t => t is ExpressionToken);
            // lookahead vars
            // TODO Generalize and stepwise patch the template structure
            templateContext = await evalJobsWithExtraVars(context, templateContext, fileId, contextData, variablesData, dict, pipelineroot, null);
            templateContext = await evalStagesWithExtraVars(context, templateContext, fileId, pipelineroot, contextData, variablesData, dict);

            templateContext = AzureDevops.CreateTemplateContext(context.TraceWriter ?? new EmptyTraceWriter(), templateContext.GetFileTable().ToArray(), context.Flags, contextData);
            if(strictParametersCheck) {
                templateContext.ExpressionValues["parameters"] = new ParametersContextData(dict, templateContext.Errors);
            }

            var evaluatedResult = TemplateEvaluator.Evaluate(templateContext, schemaName ?? "pipeline-root", pipelineroot, 0, fileId);
            templateContext.Errors.Check();
            context.TraceWriter?.Verbose("{0}", evaluatedResult.ToContextData().ToJToken().ToString());
            return evaluatedResult.AssertMapping("root");

            async Task<TemplateContext> evalJobsWithExtraVars(Context context, TemplateContext templateContext, int fileId, DictionaryContextData contextData, DictionaryContextData variablesData, Dictionary<string, object> dict, GitHub.DistributedTask.Expressions2.Sdk.IReadOnlyObject stage, DictionaryContextData vardata)
            {
                if (stage.TryGetValue("jobs", out var rjobs) && rjobs is SequenceToken jobs)
                {
                    foreach (var ji in jobs.ToArray().Select((t, i) => new { t, i }))
                    {
                        var rjob = ji.t;
                        if (rjob is GitHub.DistributedTask.Expressions2.Sdk.IReadOnlyObject job && job.TryGetValue("variables", out var rjvars) && rjvars is TemplateToken jvars)
                        {
                            templateContext = AzureDevops.CreateTemplateContext(context.TraceWriter ?? new EmptyTraceWriter(), templateContext.GetFileTable().ToArray(), context.Flags, contextData);
                            if(strictParametersCheck) {
                                templateContext.ExpressionValues["parameters"] = new ParametersContextData(dict, templateContext.Errors);
                            }
                            if(vardata != null) {
                                templateContext.ExpressionValues["variables"] = vardata;
                            }
                            IDictionary<string, VariableValue> pjvars = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
                            jvars = await ParseVariables(childContext, pjvars, jvars, templateContext);
                            var varjdata = (vardata ?? variablesData).Clone() as DictionaryContextData;
                            foreach (var v in pjvars)
                            {
                                varjdata[v.Key] = new StringContextData(v.Value.Value);
                            }
                            templateContext = AzureDevops.CreateTemplateContext(context.TraceWriter ?? new EmptyTraceWriter(), templateContext.GetFileTable().ToArray(), context.Flags, contextData);
                            if(strictParametersCheck) {
                                templateContext.ExpressionValues["parameters"] = new ParametersContextData(dict, templateContext.Errors);
                            }
                            templateContext.ExpressionValues["variables"] = varjdata;
                            (rjob as MappingToken)[(rjob as MappingToken).Select((x, i) => (x, i)).First(x => x.x.Key.ToString() == "variables").i] = new KeyValuePair<ScalarToken, TemplateToken>(new StringToken(null, null, null, "variables"), jvars);
                            rjob = TemplateEvaluator.Evaluate(templateContext, "workflow-value", rjob, 0, fileId);
                            templateContext.Errors.Check();
                            jobs[ji.i] = rjob;
                        }
                    }
                }

                return templateContext;
            }

            async Task<TemplateContext> evalStagesWithExtraVars(Context context, TemplateContext templateContext, int fileId, MappingToken pipelineroot, DictionaryContextData contextData, DictionaryContextData variablesData, Dictionary<string, object> dict)
            {
                if ((pipelineroot as GitHub.DistributedTask.Expressions2.Sdk.IReadOnlyObject).TryGetValue("stages", out var rstages) && rstages is SequenceToken stages)
                {
                    foreach (var si in stages.ToArray().Select((t, i) => new { t, i }))
                    {
                        var rstage = si.t;
                        if (rstage is GitHub.DistributedTask.Expressions2.Sdk.IReadOnlyObject stage && stage.TryGetValue("variables", out var rvars) && rvars is TemplateToken vars)
                        {
                            templateContext = AzureDevops.CreateTemplateContext(context.TraceWriter ?? new EmptyTraceWriter(), templateContext.GetFileTable().ToArray(), context.Flags, contextData);
                            if(strictParametersCheck) {
                                templateContext.ExpressionValues["parameters"] = new ParametersContextData(dict, templateContext.Errors);
                            }
                            IDictionary<string, VariableValue> pvars = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
                            vars = await ParseVariables(childContext, pvars, vars, templateContext);
                            var vardata = variablesData.Clone() as DictionaryContextData;
                            foreach (var v in pvars)
                            {
                                vardata[v.Key] = new StringContextData(v.Value.Value);
                            }
                            templateContext = await evalJobsWithExtraVars(context, templateContext, fileId, contextData, variablesData, dict, stage, vardata);
                            templateContext = AzureDevops.CreateTemplateContext(context.TraceWriter ?? new EmptyTraceWriter(), templateContext.GetFileTable().ToArray(), context.Flags, contextData);
                            if(strictParametersCheck) {
                                templateContext.ExpressionValues["parameters"] = new ParametersContextData(dict, templateContext.Errors);
                            }
                            templateContext.ExpressionValues["variables"] = vardata;
                            (rstage as MappingToken)[(rstage as MappingToken).Select((x, i) => (x, i)).First(x => x.x.Key.ToString() == "variables").i] = new KeyValuePair<ScalarToken, TemplateToken>(new StringToken(null, null, null, "variables"), vars);
                            rstage = TemplateEvaluator.Evaluate(templateContext, "workflow-value", rstage, 0, fileId);
                            templateContext.Errors.Check();
                            stages[si.i] = rstage;
                        }
                    }
                }

                return templateContext;
            }
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
}
