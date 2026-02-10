using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.Actions.WorkflowParser;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using ObjectTemplating = GitHub.DistributedTask.ObjectTemplating;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(ActionManifestManagerWrapper))]
    public interface IActionManifestManagerWrapper : IRunnerService
    {
        ActionDefinitionData Load(IExecutionContext executionContext, string manifestFile);

        DictionaryContextData EvaluateCompositeOutputs(IExecutionContext executionContext, TemplateToken token, IDictionary<string, PipelineContextData> extraExpressionValues);

        List<string> EvaluateContainerArguments(IExecutionContext executionContext, SequenceToken token, IDictionary<string, PipelineContextData> extraExpressionValues);

        Dictionary<string, string> EvaluateContainerEnvironment(IExecutionContext executionContext, MappingToken token, IDictionary<string, PipelineContextData> extraExpressionValues);

        string EvaluateDefaultInput(IExecutionContext executionContext, string inputName, TemplateToken token);
    }

    public sealed class ActionManifestManagerWrapper : RunnerService, IActionManifestManagerWrapper
    {
        private IActionManifestManagerLegacy _legacyManager;
        private IActionManifestManager _newManager;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _legacyManager = hostContext.GetService<IActionManifestManagerLegacy>();
            _newManager = hostContext.GetService<IActionManifestManager>();
        }

        public ActionDefinitionData Load(IExecutionContext executionContext, string manifestFile)
        {
            return EvaluateAndCompare(
                executionContext,
                "Load",
                () => _legacyManager.Load(executionContext, manifestFile),
                () => ConvertToLegacyActionDefinitionData(_newManager.Load(executionContext, manifestFile)),
                (legacyResult, newResult) => CompareActionDefinition(legacyResult, newResult));
        }

        public DictionaryContextData EvaluateCompositeOutputs(
            IExecutionContext executionContext,
            TemplateToken token,
            IDictionary<string, PipelineContextData> extraExpressionValues)
        {
            return EvaluateAndCompare(
                executionContext,
                "EvaluateCompositeOutputs",
                () => _legacyManager.EvaluateCompositeOutputs(executionContext, token, extraExpressionValues),
                () => ConvertToLegacyContextData<DictionaryContextData>(_newManager.EvaluateCompositeOutputs(executionContext, ConvertToNewToken(token), ConvertToNewExpressionValues(extraExpressionValues))),
                (legacyResult, newResult) => CompareDictionaryContextData(legacyResult, newResult));
        }

        public List<string> EvaluateContainerArguments(
            IExecutionContext executionContext,
            SequenceToken token,
            IDictionary<string, PipelineContextData> extraExpressionValues)
        {
            return EvaluateAndCompare(
                executionContext,
                "EvaluateContainerArguments",
                () => _legacyManager.EvaluateContainerArguments(executionContext, token, extraExpressionValues),
                () => _newManager.EvaluateContainerArguments(executionContext, ConvertToNewToken(token) as GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens.SequenceToken, ConvertToNewExpressionValues(extraExpressionValues)),
                (legacyResult, newResult) => CompareLists(legacyResult, newResult, "ContainerArguments"));
        }

        public Dictionary<string, string> EvaluateContainerEnvironment(
            IExecutionContext executionContext,
            MappingToken token,
            IDictionary<string, PipelineContextData> extraExpressionValues)
        {
            return EvaluateAndCompare(
                executionContext,
                "EvaluateContainerEnvironment",
                () => _legacyManager.EvaluateContainerEnvironment(executionContext, token, extraExpressionValues),
                () => _newManager.EvaluateContainerEnvironment(executionContext, ConvertToNewToken(token) as GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens.MappingToken, ConvertToNewExpressionValues(extraExpressionValues)),
                (legacyResult, newResult) =>
                {
                    var trace = HostContext.GetTrace(nameof(ActionManifestManagerWrapper));
                    return CompareDictionaries(trace, legacyResult, newResult, "ContainerEnvironment");
                });
        }

        public string EvaluateDefaultInput(
            IExecutionContext executionContext,
            string inputName,
            TemplateToken token)
        {
            return EvaluateAndCompare(
                executionContext,
                "EvaluateDefaultInput",
                () => _legacyManager.EvaluateDefaultInput(executionContext, inputName, token),
                () => _newManager.EvaluateDefaultInput(executionContext, inputName, ConvertToNewToken(token)),
                (legacyResult, newResult) => string.Equals(legacyResult, newResult, StringComparison.Ordinal));
        }

        // Conversion helper methods
        private ActionDefinitionData ConvertToLegacyActionDefinitionData(ActionDefinitionDataNew newData)
        {
            if (newData == null)
            {
                return null;
            }

            return new ActionDefinitionData
            {
                Name = newData.Name,
                Description = newData.Description,
                Inputs = ConvertToLegacyToken<MappingToken>(newData.Inputs),
                Deprecated = newData.Deprecated,
                Execution = ConvertToLegacyExecution(newData.Execution)
            };
        }

        private ActionExecutionData ConvertToLegacyExecution(ActionExecutionData execution)
        {
            if (execution == null)
            {
                return null;
            }

            // Handle different execution types
            if (execution is ContainerActionExecutionDataNew containerNew)
            {
                return new ContainerActionExecutionData
                {
                    Image = containerNew.Image,
                    EntryPoint = containerNew.EntryPoint,
                    Arguments = ConvertToLegacyToken<SequenceToken>(containerNew.Arguments),
                    Environment = ConvertToLegacyToken<MappingToken>(containerNew.Environment),
                    Pre = containerNew.Pre,
                    Post = containerNew.Post,
                    InitCondition = containerNew.InitCondition,
                    CleanupCondition = containerNew.CleanupCondition
                };
            }
            else if (execution is CompositeActionExecutionDataNew compositeNew)
            {
                return new CompositeActionExecutionData
                {
                    Steps = ConvertToLegacySteps(compositeNew.Steps),
                    Outputs = ConvertToLegacyToken<MappingToken>(compositeNew.Outputs)
                };
            }
            else
            {
                // For NodeJS and Plugin execution, they don't use new token types, so just return as-is
                return execution;
            }
        }

        private List<GitHub.DistributedTask.Pipelines.ActionStep> ConvertToLegacySteps(List<GitHub.Actions.WorkflowParser.IStep> newSteps)
        {
            if (newSteps == null)
            {
                return null;
            }

            var result = new List<GitHub.DistributedTask.Pipelines.ActionStep>();
            foreach (var step in newSteps)
            {
                var actionStep = new GitHub.DistributedTask.Pipelines.ActionStep
                {
                    ContextName = step.Id,
                };

                if (step is GitHub.Actions.WorkflowParser.RunStep runStep)
                {
                    actionStep.Condition = ExtractConditionString(runStep.If);
                    actionStep.DisplayNameToken = ConvertToLegacyToken<TemplateToken>(runStep.Name);
                    actionStep.ContinueOnError = ConvertToLegacyToken<TemplateToken>(runStep.ContinueOnError);
                    actionStep.TimeoutInMinutes = ConvertToLegacyToken<TemplateToken>(runStep.TimeoutMinutes);
                    actionStep.Environment = ConvertToLegacyToken<TemplateToken>(runStep.Env);
                    actionStep.Reference = new GitHub.DistributedTask.Pipelines.ScriptReference();
                    actionStep.Inputs = BuildRunStepInputs(runStep);
                }
                else if (step is GitHub.Actions.WorkflowParser.ActionStep usesStep)
                {
                    actionStep.Condition = ExtractConditionString(usesStep.If);
                    actionStep.DisplayNameToken = ConvertToLegacyToken<TemplateToken>(usesStep.Name);
                    actionStep.ContinueOnError = ConvertToLegacyToken<TemplateToken>(usesStep.ContinueOnError);
                    actionStep.TimeoutInMinutes = ConvertToLegacyToken<TemplateToken>(usesStep.TimeoutMinutes);
                    actionStep.Environment = ConvertToLegacyToken<TemplateToken>(usesStep.Env);
                    actionStep.Reference = ParseActionReference(usesStep.Uses?.Value);
                    actionStep.Inputs = ConvertToLegacyToken<MappingToken>(usesStep.With);
                }

                result.Add(actionStep);
            }
            return result;
        }

        private string ExtractConditionString(GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens.BasicExpressionToken ifToken)
        {
            if (ifToken == null)
            {
                return null;
            }

            // The Expression property is internal, so we use ToString() which formats as "${{ expr }}"
            // Then strip the delimiters to get just the expression
            var str = ifToken.ToString();
            if (str.StartsWith("${{") && str.EndsWith("}}"))
            {
                return str.Substring(3, str.Length - 5).Trim();
            }
            return str;
        }

        private MappingToken BuildRunStepInputs(GitHub.Actions.WorkflowParser.RunStep runStep)
        {
            var inputs = new MappingToken(null, null, null);

            // script (from run)
            if (runStep.Run != null)
            {
                inputs.Add(
                    new StringToken(null, null, null, "script"),
                    ConvertToLegacyToken<TemplateToken>(runStep.Run));
            }

            // shell
            if (runStep.Shell != null)
            {
                inputs.Add(
                    new StringToken(null, null, null, "shell"),
                    ConvertToLegacyToken<TemplateToken>(runStep.Shell));
            }

            // working-directory
            if (runStep.WorkingDirectory != null)
            {
                inputs.Add(
                    new StringToken(null, null, null, "workingDirectory"),
                    ConvertToLegacyToken<TemplateToken>(runStep.WorkingDirectory));
            }

            return inputs.Count > 0 ? inputs : null;
        }

        private GitHub.DistributedTask.Pipelines.ActionStepDefinitionReference ParseActionReference(string uses)
        {
            if (string.IsNullOrEmpty(uses))
            {
                return null;
            }

            // Docker reference: docker://image:tag
            if (uses.StartsWith("docker://", StringComparison.OrdinalIgnoreCase))
            {
                return new GitHub.DistributedTask.Pipelines.ContainerRegistryReference
                {
                    Image = uses.Substring("docker://".Length)
                };
            }

            // Local path reference: ./path/to/action
            if (uses.StartsWith("./") || uses.StartsWith(".\\"))
            {
                return new GitHub.DistributedTask.Pipelines.RepositoryPathReference
                {
                    RepositoryType = "self",
                    Path = uses
                };
            }

            // Repository reference: owner/repo@ref or owner/repo/path@ref
            var atIndex = uses.LastIndexOf('@');
            string refPart = null;
            string repoPart = uses;

            if (atIndex > 0)
            {
                refPart = uses.Substring(atIndex + 1);
                repoPart = uses.Substring(0, atIndex);
            }

            // Split by / to get owner/repo and optional path
            var parts = repoPart.Split('/');
            string name;
            string path = null;

            if (parts.Length >= 2)
            {
                name = $"{parts[0]}/{parts[1]}";
                if (parts.Length > 2)
                {
                    path = string.Join("/", parts, 2, parts.Length - 2);
                }
            }
            else
            {
                name = repoPart;
            }

            return new GitHub.DistributedTask.Pipelines.RepositoryPathReference
            {
                RepositoryType = "GitHub",
                Name = name,
                Ref = refPart,
                Path = path
            };
        }

        private T ConvertToLegacyToken<T>(GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens.TemplateToken newToken) where T : TemplateToken
        {
            if (newToken == null)
            {
                return null;
            }

            // Serialize and deserialize to convert between token types
            var json = StringUtil.ConvertToJson(newToken, Newtonsoft.Json.Formatting.None);
            return StringUtil.ConvertFromJson<T>(json);
        }

        private GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens.TemplateToken ConvertToNewToken(TemplateToken legacyToken)
        {
            if (legacyToken == null)
            {
                return null;
            }

            var json = StringUtil.ConvertToJson(legacyToken, Newtonsoft.Json.Formatting.None);
            return StringUtil.ConvertFromJson<GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens.TemplateToken>(json);
        }

        private IDictionary<string, GitHub.Actions.Expressions.Data.ExpressionData> ConvertToNewExpressionValues(IDictionary<string, PipelineContextData> legacyValues)
        {
            if (legacyValues == null)
            {
                return null;
            }

            var json = StringUtil.ConvertToJson(legacyValues, Newtonsoft.Json.Formatting.None);
            return StringUtil.ConvertFromJson<IDictionary<string, GitHub.Actions.Expressions.Data.ExpressionData>>(json);
        }

        private T ConvertToLegacyContextData<T>(GitHub.Actions.Expressions.Data.ExpressionData newData) where T : PipelineContextData
        {
            if (newData == null)
            {
                return null;
            }

            var json = StringUtil.ConvertToJson(newData, Newtonsoft.Json.Formatting.None);
            return StringUtil.ConvertFromJson<T>(json);
        }

        // Comparison helper methods
        private TLegacy EvaluateAndCompare<TLegacy, TNew>(
            IExecutionContext context,
            string methodName,
            Func<TLegacy> legacyEvaluator,
            Func<TNew> newEvaluator,
            Func<TLegacy, TNew, bool> resultComparer)
        {
            // Legacy only?
            if (!((context.Global.Variables.GetBoolean(Constants.Runner.Features.CompareWorkflowParser) ?? false)
                || StringUtil.ConvertToBoolean(Environment.GetEnvironmentVariable("ACTIONS_RUNNER_COMPARE_WORKFLOW_PARSER"))))
            {
                return legacyEvaluator();
            }

            var trace = HostContext.GetTrace(nameof(ActionManifestManagerWrapper));

            // Legacy evaluator
            var legacyException = default(Exception);
            var legacyResult = default(TLegacy);
            try
            {
                legacyResult = legacyEvaluator();
            }
            catch (Exception ex)
            {
                legacyException = ex;
            }

            // Compare with new evaluator
            try
            {
                ArgUtil.NotNull(context, nameof(context));
                trace.Info(methodName);

                // New evaluator
                var newException = default(Exception);
                var newResult = default(TNew);
                try
                {
                    newResult = newEvaluator();
                }
                catch (Exception ex)
                {
                    newException = ex;
                }

                // Compare results or exceptions
                if (legacyException != null || newException != null)
                {
                    // Either one or both threw exceptions - compare them
                    if (!CompareExceptions(trace, legacyException, newException))
                    {
                        trace.Info($"{methodName} exception mismatch");
                        RecordMismatch(context, $"{methodName}");
                    }
                }
                else
                {
                    // Both succeeded - compare results
                    // Skip comparison if new implementation returns null (not yet implemented)
                    if (newResult != null && !resultComparer(legacyResult, newResult))
                    {
                        trace.Info($"{methodName} mismatch");
                        RecordMismatch(context, $"{methodName}");
                    }
                }
            }
            catch (Exception ex)
            {
                trace.Info($"Comparison failed: {ex.Message}");
                RecordComparisonError(context, $"{methodName}: {ex.Message}");
            }

            // Re-throw legacy exception if any
            if (legacyException != null)
            {
                throw legacyException;
            }

            return legacyResult;
        }

        private void RecordMismatch(IExecutionContext context, string methodName)
        {
            if (!context.Global.HasActionManifestMismatch)
            {
                context.Global.HasActionManifestMismatch = true;
                var telemetry = new JobTelemetry { Type = JobTelemetryType.General, Message = $"ActionManifestMismatch: {methodName}" };
                context.Global.JobTelemetry.Add(telemetry);
            }
        }

        private void RecordComparisonError(IExecutionContext context, string errorDetails)
        {
            if (!context.Global.HasActionManifestMismatch)
            {
                context.Global.HasActionManifestMismatch = true;
                var telemetry = new JobTelemetry { Type = JobTelemetryType.General, Message = $"ActionManifestComparisonError: {errorDetails}" };
                context.Global.JobTelemetry.Add(telemetry);
            }
        }

        private bool CompareActionDefinition(ActionDefinitionData legacyResult, ActionDefinitionData newResult)
        {
            var trace = HostContext.GetTrace(nameof(ActionManifestManagerWrapper));
            if (legacyResult == null && newResult == null)
            {
                return true;
            }

            if (legacyResult == null || newResult == null)
            {
                trace.Info($"CompareActionDefinition mismatch - one result is null (legacy={legacyResult == null}, new={newResult == null})");
                return false;
            }

            if (!string.Equals(legacyResult.Name, newResult.Name, StringComparison.Ordinal))
            {
                trace.Info($"CompareActionDefinition mismatch - Name differs (legacy='{legacyResult.Name}', new='{newResult.Name}')");
                return false;
            }

            if (!string.Equals(legacyResult.Description, newResult.Description, StringComparison.Ordinal))
            {
                trace.Info($"CompareActionDefinition mismatch - Description differs (legacy='{legacyResult.Description}', new='{newResult.Description}')");
                return false;
            }

            // Compare Inputs token
            var legacyInputsJson = legacyResult.Inputs != null ? StringUtil.ConvertToJson(legacyResult.Inputs) : null;
            var newInputsJson = newResult.Inputs != null ? StringUtil.ConvertToJson(newResult.Inputs) : null;
            if (!string.Equals(legacyInputsJson, newInputsJson, StringComparison.Ordinal))
            {
                trace.Info($"CompareActionDefinition mismatch - Inputs differ");
                return false;
            }

            // Compare Deprecated
            if (!CompareDictionaries(trace, legacyResult.Deprecated, newResult.Deprecated, "Deprecated"))
            {
                return false;
            }

            // Compare Execution
            if (!CompareExecution(trace, legacyResult.Execution, newResult.Execution))
            {
                return false;
            }

            return true;
        }

        private bool CompareExecution(Tracing trace, ActionExecutionData legacy, ActionExecutionData newExecution)
        {
            if (legacy == null && newExecution == null)
            {
                return true;
            }

            if (legacy == null || newExecution == null)
            {
                trace.Info($"CompareExecution mismatch - one is null (legacy={legacy == null}, new={newExecution == null})");
                return false;
            }

            if (legacy.GetType() != newExecution.GetType())
            {
                trace.Info($"CompareExecution mismatch - different types (legacy={legacy.GetType().Name}, new={newExecution.GetType().Name})");
                return false;
            }

            // Compare based on type
            if (legacy is NodeJSActionExecutionData legacyNode && newExecution is NodeJSActionExecutionData newNode)
            {
                return CompareNodeJSExecution(trace, legacyNode, newNode);
            }
            else if (legacy is ContainerActionExecutionData legacyContainer && newExecution is ContainerActionExecutionData newContainer)
            {
                return CompareContainerExecution(trace, legacyContainer, newContainer);
            }
            else if (legacy is CompositeActionExecutionData legacyComposite && newExecution is CompositeActionExecutionData newComposite)
            {
                return CompareCompositeExecution(trace, legacyComposite, newComposite);
            }
            else if (legacy is PluginActionExecutionData legacyPlugin && newExecution is PluginActionExecutionData newPlugin)
            {
                return ComparePluginExecution(trace, legacyPlugin, newPlugin);
            }

            return true;
        }

        private bool CompareNodeJSExecution(Tracing trace, NodeJSActionExecutionData legacy, NodeJSActionExecutionData newExecution)
        {
            if (!string.Equals(legacy.NodeVersion, newExecution.NodeVersion, StringComparison.Ordinal))
            {
                trace.Info($"CompareNodeJSExecution mismatch - NodeVersion differs (legacy='{legacy.NodeVersion}', new='{newExecution.NodeVersion}')");
                return false;
            }

            if (!string.Equals(legacy.Script, newExecution.Script, StringComparison.Ordinal))
            {
                trace.Info($"CompareNodeJSExecution mismatch - Script differs (legacy='{legacy.Script}', new='{newExecution.Script}')");
                return false;
            }

            if (!string.Equals(legacy.Pre, newExecution.Pre, StringComparison.Ordinal))
            {
                trace.Info($"CompareNodeJSExecution mismatch - Pre differs");
                return false;
            }

            if (!string.Equals(legacy.Post, newExecution.Post, StringComparison.Ordinal))
            {
                trace.Info($"CompareNodeJSExecution mismatch - Post differs");
                return false;
            }

            if (!string.Equals(legacy.InitCondition, newExecution.InitCondition, StringComparison.Ordinal))
            {
                trace.Info($"CompareNodeJSExecution mismatch - InitCondition differs");
                return false;
            }

            if (!string.Equals(legacy.CleanupCondition, newExecution.CleanupCondition, StringComparison.Ordinal))
            {
                trace.Info($"CompareNodeJSExecution mismatch - CleanupCondition differs");
                return false;
            }

            return true;
        }

        private bool CompareContainerExecution(Tracing trace, ContainerActionExecutionData legacy, ContainerActionExecutionData newExecution)
        {
            if (!string.Equals(legacy.Image, newExecution.Image, StringComparison.Ordinal))
            {
                trace.Info($"CompareContainerExecution mismatch - Image differs");
                return false;
            }

            if (!string.Equals(legacy.EntryPoint, newExecution.EntryPoint, StringComparison.Ordinal))
            {
                trace.Info($"CompareContainerExecution mismatch - EntryPoint differs");
                return false;
            }

            // Compare Arguments token
            var legacyArgsJson = legacy.Arguments != null ? StringUtil.ConvertToJson(legacy.Arguments) : null;
            var newArgsJson = newExecution.Arguments != null ? StringUtil.ConvertToJson(newExecution.Arguments) : null;
            if (!string.Equals(legacyArgsJson, newArgsJson, StringComparison.Ordinal))
            {
                trace.Info($"CompareContainerExecution mismatch - Arguments differ");
                return false;
            }

            // Compare Environment token
            var legacyEnvJson = legacy.Environment != null ? StringUtil.ConvertToJson(legacy.Environment) : null;
            var newEnvJson = newExecution.Environment != null ? StringUtil.ConvertToJson(newExecution.Environment) : null;
            if (!string.Equals(legacyEnvJson, newEnvJson, StringComparison.Ordinal))
            {
                trace.Info($"CompareContainerExecution mismatch - Environment differs");
                return false;
            }

            return true;
        }

        private bool CompareCompositeExecution(Tracing trace, CompositeActionExecutionData legacy, CompositeActionExecutionData newExecution)
        {
            // Compare Steps
            if (legacy.Steps?.Count != newExecution.Steps?.Count)
            {
                trace.Info($"CompareCompositeExecution mismatch - Steps.Count differs (legacy={legacy.Steps?.Count}, new={newExecution.Steps?.Count})");
                return false;
            }

            // Compare Outputs token
            var legacyOutputsJson = legacy.Outputs != null ? StringUtil.ConvertToJson(legacy.Outputs) : null;
            var newOutputsJson = newExecution.Outputs != null ? StringUtil.ConvertToJson(newExecution.Outputs) : null;
            if (!string.Equals(legacyOutputsJson, newOutputsJson, StringComparison.Ordinal))
            {
                trace.Info($"CompareCompositeExecution mismatch - Outputs differ");
                return false;
            }

            return true;
        }

        private bool ComparePluginExecution(Tracing trace, PluginActionExecutionData legacy, PluginActionExecutionData newExecution)
        {
            if (!string.Equals(legacy.Plugin, newExecution.Plugin, StringComparison.Ordinal))
            {
                trace.Info($"ComparePluginExecution mismatch - Plugin differs");
                return false;
            }

            return true;
        }

        private bool CompareDictionaryContextData(DictionaryContextData legacy, DictionaryContextData newData)
        {
            var trace = HostContext.GetTrace(nameof(ActionManifestManagerWrapper));
            if (legacy == null && newData == null)
            {
                return true;
            }

            if (legacy == null || newData == null)
            {
                trace.Info($"CompareDictionaryContextData mismatch - one is null (legacy={legacy == null}, new={newData == null})");
                return false;
            }

            var legacyJson = StringUtil.ConvertToJson(legacy);
            var newJson = StringUtil.ConvertToJson(newData);

            if (!string.Equals(legacyJson, newJson, StringComparison.Ordinal))
            {
                trace.Info($"CompareDictionaryContextData mismatch");
                return false;
            }

            return true;
        }

        private bool CompareLists(IList<string> legacyList, IList<string> newList, string fieldName)
        {
            var trace = HostContext.GetTrace(nameof(ActionManifestManagerWrapper));
            if (legacyList == null && newList == null)
            {
                return true;
            }

            if (legacyList == null || newList == null)
            {
                trace.Info($"CompareLists mismatch - {fieldName} - one is null (legacy={legacyList == null}, new={newList == null})");
                return false;
            }

            if (legacyList.Count != newList.Count)
            {
                trace.Info($"CompareLists mismatch - {fieldName}.Count differs (legacy={legacyList.Count}, new={newList.Count})");
                return false;
            }

            for (int i = 0; i < legacyList.Count; i++)
            {
                if (!string.Equals(legacyList[i], newList[i], StringComparison.Ordinal))
                {
                    trace.Info($"CompareLists mismatch - {fieldName}[{i}] differs (legacy='{legacyList[i]}', new='{newList[i]}')");
                    return false;
                }
            }

            return true;
        }

        private bool CompareDictionaries(Tracing trace, IDictionary<string, string> legacyDict, IDictionary<string, string> newDict, string fieldName)
        {
            if (legacyDict == null && newDict == null)
            {
                return true;
            }

            if (legacyDict == null || newDict == null)
            {
                trace.Info($"CompareDictionaries mismatch - {fieldName} - one is null (legacy={legacyDict == null}, new={newDict == null})");
                return false;
            }

            if (legacyDict is Dictionary<string, string> legacyTypedDict && newDict is Dictionary<string, string> newTypedDict)
            {
                if (!object.Equals(legacyTypedDict.Comparer, newTypedDict.Comparer))
                {
                    trace.Info($"CompareDictionaries mismatch - {fieldName} - different comparers (legacy={legacyTypedDict.Comparer.GetType().Name}, new={newTypedDict.Comparer.GetType().Name})");
                    return false;
                }
            }

            if (legacyDict.Count != newDict.Count)
            {
                trace.Info($"CompareDictionaries mismatch - {fieldName}.Count differs (legacy={legacyDict.Count}, new={newDict.Count})");
                return false;
            }

            foreach (var kvp in legacyDict)
            {
                if (!newDict.TryGetValue(kvp.Key, out var newValue))
                {
                    trace.Info($"CompareDictionaries mismatch - {fieldName} - key '{kvp.Key}' missing in new result");
                    return false;
                }

                if (!string.Equals(kvp.Value, newValue, StringComparison.Ordinal))
                {
                    trace.Info($"CompareDictionaries mismatch - {fieldName}['{kvp.Key}'] differs (legacy='{kvp.Value}', new='{newValue}')");
                    return false;
                }
            }

            return true;
        }

        private bool CompareExceptions(Tracing trace, Exception legacyException, Exception newException)
        {
            if (legacyException == null && newException == null)
            {
                return true;
            }

            if (legacyException == null || newException == null)
            {
                trace.Info($"CompareExceptions mismatch - one exception is null (legacy={legacyException == null}, new={newException == null})");
                return false;
            }

            // Check for known equivalent error patterns (e.g., JSON parse errors)
            // where both parsers correctly reject invalid input but with different wording
            if (PipelineTemplateEvaluatorWrapper.HasJsonExceptionType(legacyException) && PipelineTemplateEvaluatorWrapper.HasJsonExceptionType(newException))
            {
                trace.Info("CompareExceptions - both exceptions are JSON parse errors, treating as matched");
                return true;
            }

            // Compare exception messages recursively (including inner exceptions)
            var legacyMessages = GetExceptionMessages(legacyException);
            var newMessages = GetExceptionMessages(newException);

            if (legacyMessages.Count != newMessages.Count)
            {
                trace.Info($"CompareExceptions mismatch - different number of exception messages (legacy={legacyMessages.Count}, new={newMessages.Count})");
                return false;
            }

            for (int i = 0; i < legacyMessages.Count; i++)
            {
                if (!string.Equals(legacyMessages[i], newMessages[i], StringComparison.Ordinal))
                {
                    trace.Info($"CompareExceptions mismatch - exception messages differ at level {i} (legacy='{legacyMessages[i]}', new='{newMessages[i]}')");
                    return false;
                }
            }

            return true;
        }

        private IList<string> GetExceptionMessages(Exception ex)
        {
            var trace = HostContext.GetTrace(nameof(ActionManifestManagerWrapper));
            var messages = new List<string>();
            var toProcess = new Queue<Exception>();
            toProcess.Enqueue(ex);
            int count = 0;

            while (toProcess.Count > 0 && count < 50)
            {
                var current = toProcess.Dequeue();
                if (current == null) continue;

                messages.Add(current.Message);
                count++;

                // Special handling for AggregateException - enqueue all inner exceptions
                if (current is AggregateException aggregateEx)
                {
                    foreach (var innerEx in aggregateEx.InnerExceptions)
                    {
                        if (innerEx != null && count < 50)
                        {
                            toProcess.Enqueue(innerEx);
                        }
                    }
                }
                else if (current.InnerException != null)
                {
                    toProcess.Enqueue(current.InnerException);
                }

                // Failsafe: if we have too many exceptions, stop and return what we have
                if (count >= 50)
                {
                    trace.Info("CompareExceptions failsafe triggered - too many exceptions (50+)");
                    break;
                }
            }

            return messages;
        }

    }
}
