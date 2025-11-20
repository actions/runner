using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Linq;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.Actions.WorkflowParser;
using GitHub.Actions.WorkflowParser.Conversion;
using GitHub.Actions.WorkflowParser.ObjectTemplating;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Schema;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;
using GitHub.Actions.Expressions.Data;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(ActionManifestManager))]
    public interface IActionManifestManager : IRunnerService
    {
        public ActionDefinitionDataNew Load(IExecutionContext executionContext, string manifestFile);

        DictionaryExpressionData EvaluateCompositeOutputs(IExecutionContext executionContext, TemplateToken token, IDictionary<string, ExpressionData> extraExpressionValues);

        List<string> EvaluateContainerArguments(IExecutionContext executionContext, SequenceToken token, IDictionary<string, ExpressionData> extraExpressionValues);

        Dictionary<string, string> EvaluateContainerEnvironment(IExecutionContext executionContext, MappingToken token, IDictionary<string, ExpressionData> extraExpressionValues);

        string EvaluateDefaultInput(IExecutionContext executionContext, string inputName, TemplateToken token);
    }

    public sealed class ActionManifestManager : RunnerService, IActionManifestManager
    {
        private TemplateSchema _actionManifestSchema;
        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            var assembly = Assembly.GetExecutingAssembly();
            var json = default(string);
            using (var stream = assembly.GetManifestResourceStream("GitHub.Runner.Worker.action_yaml.json"))
            using (var streamReader = new StreamReader(stream))
            {
                json = streamReader.ReadToEnd();
            }

            var objectReader = new JsonObjectReader(null, json);
            _actionManifestSchema = TemplateSchema.Load(objectReader);
            ArgUtil.NotNull(_actionManifestSchema, nameof(_actionManifestSchema));
            Trace.Info($"Load schema file with definitions: {StringUtil.ConvertToJson(_actionManifestSchema.Definitions.Keys)}");
        }

        public ActionDefinitionDataNew Load(IExecutionContext executionContext, string manifestFile)
        {
            var templateContext = CreateTemplateContext(executionContext);
            ActionDefinitionDataNew actionDefinition = new();

            // Clean up file name real quick
            // Instead of using Regex which can be computationally expensive,
            // we can just remove the # of characters from the fileName according to the length of the basePath
            string basePath = HostContext.GetDirectory(WellKnownDirectory.Actions);
            string fileRelativePath = manifestFile;
            if (manifestFile.Contains(basePath))
            {
                fileRelativePath = manifestFile.Remove(0, basePath.Length + 1);
            }

            try
            {
                var token = default(TemplateToken);

                // Get the file ID
                var fileId = templateContext.GetFileId(fileRelativePath);

                // Add this file to the FileTable in executionContext if it hasn't been added already
                // we use > since fileID is 1 indexed
                if (fileId > executionContext.Global.FileTable.Count)
                {
                    executionContext.Global.FileTable.Add(fileRelativePath);
                }

                // Read the file
                var fileContent = File.ReadAllText(manifestFile);
                using (var stringReader = new StringReader(fileContent))
                {
                    var yamlObjectReader = new YamlObjectReader(fileId, stringReader);
                    token = TemplateReader.Read(templateContext, "action-root", yamlObjectReader, fileId, out _);
                }

                var actionMapping = token.AssertMapping("action manifest root");
                var actionOutputs = default(MappingToken);
                var actionRunValueToken = default(TemplateToken);

                foreach (var actionPair in actionMapping)
                {
                    var propertyName = actionPair.Key.AssertString($"action.yml property key");

                    switch (propertyName.Value)
                    {
                        case "name":
                            actionDefinition.Name = actionPair.Value.AssertString("name").Value;
                            break;

                        case "outputs":
                            actionOutputs = actionPair.Value.AssertMapping("outputs");
                            break;

                        case "description":
                            actionDefinition.Description = actionPair.Value.AssertString("description").Value;
                            break;

                        case "inputs":
                            ConvertInputs(actionPair.Value, actionDefinition);
                            break;

                        case "runs":
                            // Defer runs token evaluation to after for loop to ensure that order of outputs doesn't matter.
                            actionRunValueToken = actionPair.Value;
                            break;

                        default:
                            Trace.Info($"Ignore action property {propertyName}.");
                            break;
                    }
                }

                // Evaluate Runs Last
                if (actionRunValueToken != null)
                {
                    actionDefinition.Execution = ConvertRuns(executionContext, templateContext, actionRunValueToken, fileRelativePath, actionOutputs);
                }
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
                templateContext.Errors.Add(ex);
            }

            if (templateContext.Errors.Count > 0)
            {
                foreach (var error in templateContext.Errors)
                {
                    Trace.Error($"Action.yml load error: {error.Message}");
                    executionContext.Error(error.Message);
                }

                throw new ArgumentException($"Failed to load {fileRelativePath}");
            }

            if (actionDefinition.Execution == null)
            {
                executionContext.Debug($"Loaded action.yml file: {StringUtil.ConvertToJson(actionDefinition)}");
                throw new ArgumentException($"Top level 'runs:' section is required for {fileRelativePath}");
            }
            else
            {
                Trace.Info($"Loaded action.yml file: {StringUtil.ConvertToJson(actionDefinition)}");
            }

            return actionDefinition;
        }

        public DictionaryExpressionData EvaluateCompositeOutputs(
            IExecutionContext executionContext,
            TemplateToken token,
            IDictionary<string, ExpressionData> extraExpressionValues)
        {
            DictionaryExpressionData result = null;

            if (token != null)
            {
                var templateContext = CreateTemplateContext(executionContext, extraExpressionValues);
                try
                {
                    token = TemplateEvaluator.Evaluate(templateContext, "outputs", token, 0, null);
                    templateContext.Errors.Check();
                    result = token.ToExpressionData().AssertDictionary("composite outputs");
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    templateContext.Errors.Add(ex);
                }

                templateContext.Errors.Check();
            }

            return result ?? new DictionaryExpressionData();
        }

        public List<string> EvaluateContainerArguments(
            IExecutionContext executionContext,
            SequenceToken token,
            IDictionary<string, ExpressionData> extraExpressionValues)
        {
            var result = new List<string>();

            if (token != null)
            {
                var templateContext = CreateTemplateContext(executionContext, extraExpressionValues);
                try
                {
                    var evaluateResult = TemplateEvaluator.Evaluate(templateContext, "container-runs-args", token, 0, null);
                    templateContext.Errors.Check();

                    Trace.Info($"Arguments evaluate result: {StringUtil.ConvertToJson(evaluateResult)}");

                    // Sequence
                    var args = evaluateResult.AssertSequence("container args");

                    foreach (var arg in args)
                    {
                        var str = arg.AssertString("container arg").Value;
                        result.Add(str);
                        Trace.Info($"Add argument {str}");
                    }
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    Trace.Error(ex);
                    templateContext.Errors.Add(ex);
                }

                templateContext.Errors.Check();
            }

            return result;
        }

        public Dictionary<string, string> EvaluateContainerEnvironment(
            IExecutionContext executionContext,
            MappingToken token,
            IDictionary<string, ExpressionData> extraExpressionValues)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (token != null)
            {
                var templateContext = CreateTemplateContext(executionContext, extraExpressionValues);
                try
                {
                    var evaluateResult = TemplateEvaluator.Evaluate(templateContext, "container-runs-env", token, 0, null);
                    templateContext.Errors.Check();

                    Trace.Info($"Environments evaluate result: {StringUtil.ConvertToJson(evaluateResult)}");

                    // Mapping
                    var mapping = evaluateResult.AssertMapping("container env");

                    foreach (var pair in mapping)
                    {
                        // Literal key
                        var key = pair.Key.AssertString("container env key");

                        // Literal value
                        var value = pair.Value.AssertString("container env value");
                        result[key.Value] = value.Value;

                        Trace.Info($"Add env {key} = {value}");
                    }
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    Trace.Error(ex);
                    templateContext.Errors.Add(ex);
                }

                templateContext.Errors.Check();
            }

            return result;
        }

        public string EvaluateDefaultInput(
            IExecutionContext executionContext,
            string inputName,
            TemplateToken token)
        {
            string result = "";
            if (token != null)
            {
                var templateContext = CreateTemplateContext(executionContext);
                try
                {
                    var evaluateResult = TemplateEvaluator.Evaluate(templateContext, "input-default-context", token, 0, null);
                    templateContext.Errors.Check();

                    Trace.Info($"Input '{inputName}': default value evaluate result: {StringUtil.ConvertToJson(evaluateResult)}");

                    // String
                    result = evaluateResult.AssertString($"default value for input '{inputName}'").Value;
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    Trace.Error(ex);
                    templateContext.Errors.Add(ex);
                }

                templateContext.Errors.Check();
            }

            return result;
        }

        private TemplateContext CreateTemplateContext(
            IExecutionContext executionContext,
            IDictionary<string, ExpressionData> extraExpressionValues = null)
        {
            var result = new TemplateContext
            {
                CancellationToken = CancellationToken.None,
                Errors = new TemplateValidationErrors(10, int.MaxValue), // Don't truncate error messages otherwise we might not scrub secrets correctly
                Memory = new TemplateMemory(
                    maxDepth: 100,
                    maxEvents: 1000000,
                    maxBytes: 10 * 1024 * 1024),
                Schema = _actionManifestSchema,
                // TODO: Switch to real tracewriter for cutover
                TraceWriter = new GitHub.Actions.WorkflowParser.ObjectTemplating.EmptyTraceWriter(),
            };

            // Expression values from execution context
            foreach (var pair in executionContext.ExpressionValues)
            {
                // Convert old PipelineContextData to new ExpressionData
                var json = StringUtil.ConvertToJson(pair.Value, Newtonsoft.Json.Formatting.None);
                var newValue = StringUtil.ConvertFromJson<GitHub.Actions.Expressions.Data.ExpressionData>(json);
                result.ExpressionValues[pair.Key] = newValue;
            }

            // Extra expression values
            if (extraExpressionValues?.Count > 0)
            {
                foreach (var pair in extraExpressionValues)
                {
                    result.ExpressionValues[pair.Key] = pair.Value;
                }
            }

            // Expression functions
            foreach (var func in executionContext.ExpressionFunctions)
            {
                GitHub.Actions.Expressions.IFunctionInfo newFunc = func.Name switch
                {
                    "always" => new GitHub.Actions.Expressions.FunctionInfo<Expressions.NewAlwaysFunction>(func.Name, func.MinParameters, func.MaxParameters),
                    "cancelled" => new GitHub.Actions.Expressions.FunctionInfo<Expressions.NewCancelledFunction>(func.Name, func.MinParameters, func.MaxParameters),
                    "failure" => new GitHub.Actions.Expressions.FunctionInfo<Expressions.NewFailureFunction>(func.Name, func.MinParameters, func.MaxParameters),
                    "success" => new GitHub.Actions.Expressions.FunctionInfo<Expressions.NewSuccessFunction>(func.Name, func.MinParameters, func.MaxParameters),
                    "hashFiles" => new GitHub.Actions.Expressions.FunctionInfo<Expressions.NewHashFilesFunction>(func.Name, func.MinParameters, func.MaxParameters),
                    _ => throw new NotSupportedException($"Expression function '{func.Name}' is not supported in ActionManifestManager")
                };
                result.ExpressionFunctions.Add(newFunc);
            }

            // Add the file table from the Execution Context
            for (var i = 0; i < executionContext.Global.FileTable.Count; i++)
            {
                result.GetFileId(executionContext.Global.FileTable[i]);
            }

            return result;
        }

        private ActionExecutionData ConvertRuns(
            IExecutionContext executionContext,
            TemplateContext templateContext,
            TemplateToken inputsToken,
            String fileRelativePath,
            MappingToken outputs = null)
        {
            var runsMapping = inputsToken.AssertMapping("runs");
            var usingToken = default(StringToken);
            var imageToken = default(StringToken);
            var argsToken = default(SequenceToken);
            var entrypointToken = default(StringToken);
            var envToken = default(MappingToken);
            var mainToken = default(StringToken);
            var pluginToken = default(StringToken);
            var preToken = default(StringToken);
            var preEntrypointToken = default(StringToken);
            var preIfToken = default(StringToken);
            var postToken = default(StringToken);
            var postEntrypointToken = default(StringToken);
            var postIfToken = default(StringToken);
            var steps = default(List<GitHub.Actions.WorkflowParser.IStep>);

            foreach (var run in runsMapping)
            {
                var runsKey = run.Key.AssertString("runs key").Value;
                switch (runsKey)
                {
                    case "using":
                        usingToken = run.Value.AssertString("using");
                        break;
                    case "image":
                        imageToken = run.Value.AssertString("image");
                        break;
                    case "args":
                        argsToken = run.Value.AssertSequence("args");
                        break;
                    case "entrypoint":
                        entrypointToken = run.Value.AssertString("entrypoint");
                        break;
                    case "env":
                        envToken = run.Value.AssertMapping("env");
                        break;
                    case "main":
                        mainToken = run.Value.AssertString("main");
                        break;
                    case "plugin":
                        pluginToken = run.Value.AssertString("plugin");
                        break;
                    case "post":
                        postToken = run.Value.AssertString("post");
                        break;
                    case "post-entrypoint":
                        postEntrypointToken = run.Value.AssertString("post-entrypoint");
                        break;
                    case "post-if":
                        postIfToken = run.Value.AssertString("post-if");
                        break;
                    case "pre":
                        preToken = run.Value.AssertString("pre");
                        break;
                    case "pre-entrypoint":
                        preEntrypointToken = run.Value.AssertString("pre-entrypoint");
                        break;
                    case "pre-if":
                        preIfToken = run.Value.AssertString("pre-if");
                        break;
                    case "steps":
                        var stepsToken = run.Value.AssertSequence("steps");
                        steps = WorkflowTemplateConverter.ConvertToSteps(templateContext, stepsToken);
                        templateContext.Errors.Check();
                        break;
                    default:
                        Trace.Info($"Ignore run property {runsKey}.");
                        break;
                }
            }

            if (usingToken != null)
            {
                if (string.Equals(usingToken.Value, "docker", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(imageToken?.Value))
                    {
                        throw new ArgumentNullException($"You are using a Container Action but an image is not provided in {fileRelativePath}.");
                    }
                    else
                    {
                        return new ContainerActionExecutionDataNew()
                        {
                            Image = imageToken.Value,
                            Arguments = argsToken,
                            EntryPoint = entrypointToken?.Value,
                            Environment = envToken,
                            Pre = preEntrypointToken?.Value,
                            InitCondition = preIfToken?.Value ?? "always()",
                            Post = postEntrypointToken?.Value,
                            CleanupCondition = postIfToken?.Value ?? "always()"
                        };
                    }
                }
                else if (string.Equals(usingToken.Value, "node12", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(usingToken.Value, "node16", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(usingToken.Value, "node20", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(usingToken.Value, "node24", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(usingToken.Value, "bun", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(mainToken?.Value))
                    {
                        throw new ArgumentNullException($"You are using a JavaScript Action but there is not an entry JavaScript file provided in {fileRelativePath}.");
                    }
                    else
                    {
                        return new NodeJSActionExecutionData()
                        {
                            NodeVersion = usingToken.Value,
                            Script = mainToken.Value,
                            Pre = preToken?.Value,
                            InitCondition = preIfToken?.Value ?? "always()",
                            Post = postToken?.Value,
                            CleanupCondition = postIfToken?.Value ?? "always()"
                        };
                    }
                }
                else if (string.Equals(usingToken.Value, "composite", StringComparison.OrdinalIgnoreCase))
                {
                    if (steps == null)
                    {
                        throw new ArgumentNullException($"You are using a composite action but there are no steps provided in {fileRelativePath}.");
                    }
                    else
                    {
                        return new CompositeActionExecutionDataNew()
                        {
                            Steps = steps,
                            PreSteps = new List<GitHub.Actions.WorkflowParser.IStep>(),
                            PostSteps = new Stack<GitHub.Actions.WorkflowParser.IStep>(),
                            InitCondition = "always()",
                            CleanupCondition = "always()",
                            Outputs = outputs
                        };
                    }
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"'using: {usingToken.Value}' is not supported, use 'docker', 'node12', 'node16', 'node20', 'node24' or 'bun' instead.");
                }
            }
            else if (pluginToken != null)
            {
                return new PluginActionExecutionData()
                {
                    Plugin = pluginToken.Value
                };
            }

            throw new NotSupportedException("Missing 'using' value. 'using' requires 'composite', 'docker', 'node12', 'node16', 'node20', 'node24' or 'bun'.");
        }

        private void ConvertInputs(
            TemplateToken inputsToken,
            ActionDefinitionDataNew actionDefinition)
        {
            actionDefinition.Inputs = new MappingToken(null, null, null);
            var inputsMapping = inputsToken.AssertMapping("inputs");
            foreach (var input in inputsMapping)
            {
                bool hasDefault = false;
                var inputName = input.Key.AssertString("input name");
                var inputMetadata = input.Value.AssertMapping("input metadata");
                foreach (var metadata in inputMetadata)
                {
                    var metadataName = metadata.Key.AssertString("input metadata").Value;
                    if (string.Equals(metadataName, "default", StringComparison.OrdinalIgnoreCase))
                    {
                        hasDefault = true;
                        actionDefinition.Inputs.Add(inputName, metadata.Value);
                    }
                    else if (string.Equals(metadataName, "deprecationMessage", StringComparison.OrdinalIgnoreCase))
                    {
                        if (actionDefinition.Deprecated == null)
                        {
                            actionDefinition.Deprecated = new Dictionary<String, String>();
                        }
                        var message = metadata.Value.AssertString("input deprecationMessage");
                        actionDefinition.Deprecated.Add(inputName.Value, message.Value);
                    }
                }

                if (!hasDefault)
                {
                    actionDefinition.Inputs.Add(inputName, new StringToken(null, null, null, string.Empty));
                }
            }
        }
    }

    public sealed class ActionDefinitionDataNew
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public MappingToken Inputs { get; set; }

        public ActionExecutionData Execution { get; set; }

        public Dictionary<String, String> Deprecated { get; set; }
    }

    public sealed class ContainerActionExecutionDataNew : ActionExecutionData
    {
        public override ActionExecutionType ExecutionType => ActionExecutionType.Container;

        public override bool HasPre => !string.IsNullOrEmpty(Pre);
        public override bool HasPost => !string.IsNullOrEmpty(Post);

        public string Image { get; set; }

        public string EntryPoint { get; set; }

        public SequenceToken Arguments { get; set; }

        public MappingToken Environment { get; set; }

        public string Pre { get; set; }

        public string Post { get; set; }
    }

    public sealed class CompositeActionExecutionDataNew : ActionExecutionData
    {
        public override ActionExecutionType ExecutionType => ActionExecutionType.Composite;
        public override bool HasPre => PreSteps.Count > 0;
        public override bool HasPost => PostSteps.Count > 0;
        public List<GitHub.Actions.WorkflowParser.IStep> PreSteps { get; set; }
        public List<GitHub.Actions.WorkflowParser.IStep> Steps { get; set; }
        public Stack<GitHub.Actions.WorkflowParser.IStep> PostSteps { get; set; }
        public MappingToken Outputs { get; set; }
    }
}
