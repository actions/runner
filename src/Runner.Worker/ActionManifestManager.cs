using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using System.Reflection;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Schema;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using System.Globalization;
using System.Linq;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(ActionManifestManager))]
    public interface IActionManifestManager : IRunnerService
    {
        ActionDefinitionData Load(IExecutionContext executionContext, string manifestFile);

        List<string> EvaluateContainerArguments(IExecutionContext executionContext, SequenceToken token, IDictionary<string, PipelineContextData> extraExpressionValues);

        Dictionary<string, string> EvaluateContainerEnvironment(IExecutionContext executionContext, MappingToken token, IDictionary<string, PipelineContextData> extraExpressionValues);

        public Dictionary<string, string> EvaluateCompositeActionEnvironment(IExecutionContext executionContext, MappingToken token, IDictionary<string, PipelineContextData> extraExpressionValues);
        string EvaluateDefaultInput(IExecutionContext executionContext, string inputName, TemplateToken token);
    }

    public sealed class ActionManifestManager : RunnerService, IActionManifestManager
    {
        private TemplateSchema _actionManifestSchema;
        private IReadOnlyList<String> _fileTable;

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

        public ActionDefinitionData Load(IExecutionContext executionContext, string manifestFile)
        {
            var context = CreateContext(executionContext);
            ActionDefinitionData actionDefinition = new ActionDefinitionData();
            try
            {
                var token = default(TemplateToken);

                // Get the file ID
                var fileId = context.GetFileId(manifestFile);
                _fileTable = context.GetFileTable();

                // Read the file
                var fileContent = File.ReadAllText(manifestFile);
                using (var stringReader = new StringReader(fileContent))
                {
                    var yamlObjectReader = new YamlObjectReader(null, stringReader);
                    token = TemplateReader.Read(context, "action-root", yamlObjectReader, fileId, out _);
                }

                var actionMapping = token.AssertMapping("action manifest root");
                foreach (var actionPair in actionMapping)
                {
                    var propertyName = actionPair.Key.AssertString($"action.yml property key");

                    switch (propertyName.Value)
                    {
                        case "name":
                            actionDefinition.Name = actionPair.Value.AssertString("name").Value;
                            break;

                        case "description":
                            actionDefinition.Description = actionPair.Value.AssertString("description").Value;
                            break;

                        case "inputs":
                            ConvertInputs(context, actionPair.Value, actionDefinition);
                            break;

                        case "runs":
                            actionDefinition.Execution = ConvertRuns(executionContext, context, actionPair.Value);
                            break;
                        default:
                            Trace.Info($"Ignore action property {propertyName}.");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
                context.Errors.Add(ex);
            }

            if (context.Errors.Count > 0)
            {
                foreach (var error in context.Errors)
                {
                    Trace.Error($"Action.yml load error: {error.Message}");
                    executionContext.Error(error.Message);
                }

                throw new ArgumentException($"Fail to load {manifestFile}");
            }

            if (actionDefinition.Execution == null)
            {
                executionContext.Debug($"Loaded action.yml file: {StringUtil.ConvertToJson(actionDefinition)}");
                throw new ArgumentException($"Top level 'runs:' section is required for {manifestFile}");
            }
            else
            {
                Trace.Info($"Loaded action.yml file: {StringUtil.ConvertToJson(actionDefinition)}");
            }

            return actionDefinition;
        }

        public List<string> EvaluateContainerArguments(
            IExecutionContext executionContext,
            SequenceToken token,
            IDictionary<string, PipelineContextData> extraExpressionValues)
        {
            var result = new List<string>();

            if (token != null)
            {
                var context = CreateContext(executionContext, extraExpressionValues);
                try
                {
                    var evaluateResult = TemplateEvaluator.Evaluate(context, "container-runs-args", token, 0, null, omitHeader: true);
                    context.Errors.Check();

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
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result;
        }

        public Dictionary<string, string> EvaluateContainerEnvironment(
            IExecutionContext executionContext,
            MappingToken token,
            IDictionary<string, PipelineContextData> extraExpressionValues)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (token != null)
            {
                var context = CreateContext(executionContext, extraExpressionValues);
                try
                {
                    var evaluateResult = TemplateEvaluator.Evaluate(context, "runs-env", token, 0, null, omitHeader: true);
                    context.Errors.Check();

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
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result;
        }

        public Dictionary<string, string> EvaluateCompositeActionEnvironment(
            IExecutionContext executionContext,
            MappingToken token,
            IDictionary<string, PipelineContextData> extraExpressionValues)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (token != null)
            {
                var context = CreateContext(executionContext, extraExpressionValues);
                try
                {
                    var evaluateResult = TemplateEvaluator.Evaluate(context, "runs-env", token, 0, null, omitHeader: true);
                    context.Errors.Check();

                    // Mapping
                    var mapping = evaluateResult.AssertMapping("composite env");

                    foreach (var pair in mapping)
                    {
                        // Literal key
                        var key = pair.Key.AssertString("composite env key");

                        // Literal value
                        var value = pair.Value.AssertString("composite env value");
                        result[key.Value] = value.Value;

                        Trace.Info($"Add env {key} = {value}");
                    }
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    Trace.Error(ex);
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
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
                var context = CreateContext(executionContext);
                try
                {
                    var evaluateResult = TemplateEvaluator.Evaluate(context, "input-default-context", token, 0, null, omitHeader: true);
                    context.Errors.Check();

                    Trace.Info($"Input '{inputName}': default value evaluate result: {StringUtil.ConvertToJson(evaluateResult)}");

                    // String
                    result = evaluateResult.AssertString($"default value for input '{inputName}'").Value;
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    Trace.Error(ex);
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result;
        }

        private TemplateContext CreateContext(
            IExecutionContext executionContext,
            IDictionary<string, PipelineContextData> extraExpressionValues = null)
        {
            var result = new TemplateContext
            {
                CancellationToken = CancellationToken.None,
                Errors = new TemplateValidationErrors(10, 500),
                Memory = new TemplateMemory(
                    maxDepth: 100,
                    maxEvents: 1000000,
                    maxBytes: 10 * 1024 * 1024),
                Schema = _actionManifestSchema,
                TraceWriter = executionContext.ToTemplateTraceWriter(),
            };

            // Expression values from execution context
            foreach (var pair in executionContext.ExpressionValues)
            {
                result.ExpressionValues[pair.Key] = pair.Value;
            }

            // Extra expression values
            if (extraExpressionValues?.Count > 0)
            {
                foreach (var pair in extraExpressionValues)
                {
                    result.ExpressionValues[pair.Key] = pair.Value;
                }
            }

            // Expression functions from execution context
            foreach (var item in executionContext.ExpressionFunctions)
            {
                result.ExpressionFunctions.Add(item);
            }

            // Add the file table
            if (_fileTable?.Count > 0)
            {
                for (var i = 0; i < _fileTable.Count; i++)
                {
                    result.GetFileId(_fileTable[i]);
                }
            }

            return result;
        }

        private ActionExecutionData ConvertRuns(
            IExecutionContext executionContext,
            TemplateContext context,
            TemplateToken inputsToken)
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
            var stepsLoaded = default(List<Pipelines.ActionStep>);

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
                        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TESTING_COMPOSITE_ACTIONS_ALPHA")))
                        {
                            var steps = run.Value.AssertSequence("steps");
                            var evaluator = executionContext.ToPipelineTemplateEvaluator();
                            stepsLoaded = evaluator.LoadCompositeSteps(steps);
                            break;
                        }
                        throw new Exception("You aren't supposed to be using Composite Actions yet!");
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
                        throw new ArgumentNullException($"Image is not provided.");
                    }
                    else
                    {
                        return new ContainerActionExecutionData()
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
                else if (string.Equals(usingToken.Value, "node12", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(mainToken?.Value))
                    {
                        throw new ArgumentNullException($"Entry javascript file is not provided.");
                    }
                    else
                    {
                        return new NodeJSActionExecutionData()
                        {
                            Script = mainToken.Value,
                            Pre = preToken?.Value,
                            InitCondition = preIfToken?.Value ?? "always()",
                            Post = postToken?.Value,
                            CleanupCondition = postIfToken?.Value ?? "always()"
                        };
                    }
                }
                else if (string.Equals(usingToken.Value, "composite", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TESTING_COMPOSITE_ACTIONS_ALPHA")))
                {
                    if (stepsLoaded == null)
                    {
                        throw new ArgumentNullException($"No steps provided.");
                    }
                    else
                    {
                        return new CompositeActionExecutionData()
                        {
                            Steps = stepsLoaded,
                            Environment = envToken
                        };
                    }
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"'using: {usingToken.Value}' is not supported, use 'docker' or 'node12' instead.");
                }
            }
            else if (pluginToken != null)
            {
                return new PluginActionExecutionData()
                {
                    Plugin = pluginToken.Value
                };
            }

            throw new NotSupportedException(nameof(ConvertRuns));
        }

        private void ConvertInputs(
            TemplateContext context,
            TemplateToken inputsToken,
            ActionDefinitionData actionDefinition)
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
}

