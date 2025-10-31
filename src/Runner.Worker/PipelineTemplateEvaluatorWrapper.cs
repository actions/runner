using System;
using System.Collections.Generic;
using GitHub.Actions.WorkflowParser;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using ObjectTemplating = GitHub.DistributedTask.ObjectTemplating;

namespace GitHub.Runner.Worker
{
    internal sealed class PipelineTemplateEvaluatorWrapper : IPipelineTemplateEvaluator
    {
        private readonly PipelineTemplateEvaluator _legacyEvaluator;
        private readonly WorkflowTemplateEvaluator _newEvaluator;
        private readonly bool _compare;
        private readonly IExecutionContext _context;

        public PipelineTemplateEvaluatorWrapper(
            IExecutionContext context,
            ObjectTemplating.ITraceWriter traceWriter = null)
        {
            ArgUtil.NotNull(context, nameof(context));
            _context = context;

            if (traceWriter == null)
            {
                traceWriter = context.ToTemplateTraceWriter();
            }

            // Compare?
            _compare = context.Global.Variables.GetBoolean(Constants.Runner.Features.CompareTemplateEvaluator) ?? false;

            // Legacy evaluator
            var schema = PipelineTemplateSchemaFactory.GetSchema();
            _legacyEvaluator = new PipelineTemplateEvaluator(traceWriter, schema, context.Global.FileTable)
            {
                MaxErrorMessageLength = int.MaxValue, // Don't truncate error messages otherwise we might not scrub secrets correctly
            };

            // New evaluator
            var newTraceWriter = new GitHub.Actions.WorkflowParser.ObjectTemplating.EmptyTraceWriter();
            _newEvaluator = new WorkflowTemplateEvaluator(newTraceWriter, context.Global.FileTable, features: null)
            {
                MaxErrorMessageLength = int.MaxValue, // Don't truncate error messages otherwise we might not scrub secrets correctly
            };
        }

        public Boolean EvaluateStepContinueOnError(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var legacyResult = _legacyEvaluator.EvaluateStepContinueOnError(token, contextData, expressionFunctions);

            if (_compare)
            {
                try
                {
                    _context.Debug("Comparing new template evaluator: EvaluateStepContinueOnError");
                    var convertedToken = ConvertToken(token);
                    var convertedData = ConvertData(contextData);
                    var convertedFunctions = ConvertFunctions(expressionFunctions);
                    var newResult = _newEvaluator.EvaluateStepContinueOnError(convertedToken, convertedData, convertedFunctions);
                    if (legacyResult != newResult)
                    {
                        _context.Debug($"Mismatch: EvaluateStepContinueOnError differs (legacy={legacyResult}, new={newResult})");
                        var telemetry = new JobTelemetry { Type = JobTelemetryType.General, Message = "TemplateEvaluatorMismatch: EvaluateStepContinueOnError" };
                        _context.Global.JobTelemetry.Add(telemetry);
                    }
                }
                catch (Exception ex)
                {
                    _context.Debug($"Template evaluator comparison failed: {ex.Message}");
                    var telemetry = new JobTelemetry { Type = JobTelemetryType.General, Message = $"TemplateEvaluatorComparisonError: EvaluateStepContinueOnError: {ex.Message}" };
                    _context.Global.JobTelemetry.Add(telemetry);
                }
            }

            return legacyResult;
        }

        public String EvaluateStepDisplayName(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var legacyResult = _legacyEvaluator.EvaluateStepDisplayName(token, contextData, expressionFunctions);

            if (_compare)
            {
                try
                {
                    _context.Debug("Comparing new template evaluator: EvaluateStepDisplayName");
                    var convertedToken = ConvertToken(token);
                    var convertedData = ConvertData(contextData);
                    var convertedFunctions = ConvertFunctions(expressionFunctions);
                    var newResult = _newEvaluator.EvaluateStepName(convertedToken, convertedData, convertedFunctions);
                    if (!string.Equals(legacyResult, newResult, StringComparison.Ordinal))
                    {
                        _context.Debug($"Mismatch: EvaluateStepDisplayName differs (legacy='{legacyResult}', new='{newResult}')");
                        var telemetry = new JobTelemetry { Type = JobTelemetryType.General, Message = "TemplateEvaluatorMismatch: EvaluateStepDisplayName" };
                        _context.Global.JobTelemetry.Add(telemetry);
                    }
                }
                catch (Exception ex)
                {
                    _context.Debug($"Template evaluator comparison failed: {ex.Message}");
                    var telemetry = new JobTelemetry { Type = JobTelemetryType.General, Message = $"TemplateEvaluatorComparisonError: EvaluateStepDisplayName: {ex.Message}" };
                    _context.Global.JobTelemetry.Add(telemetry);
                }
            }

            return legacyResult;
        }

        public Dictionary<String, String> EvaluateStepEnvironment(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions,
            StringComparer keyComparer)
        {
            var legacyResult = _legacyEvaluator.EvaluateStepEnvironment(token, contextData, expressionFunctions, keyComparer);

            if (_compare)
            {
                try
                {
                    _context.Debug("Comparing new template evaluator: EvaluateStepEnvironment");
                    var convertedToken = ConvertToken(token);
                    var convertedData = ConvertData(contextData);
                    var convertedFunctions = ConvertFunctions(expressionFunctions);
                    var newResult = _newEvaluator.EvaluateStepEnvironment(convertedToken, convertedData, convertedFunctions, keyComparer);
                    if (!CompareStepEnvironment(legacyResult, newResult))
                    {
                        var telemetry = new JobTelemetry { Type = JobTelemetryType.General, Message = "TemplateEvaluatorMismatch: EvaluateStepEnvironment" };
                        _context.Global.JobTelemetry.Add(telemetry);
                    }
                }
                catch (Exception ex)
                {
                    _context.Debug($"Template evaluator comparison failed: {ex.Message}");
                    var telemetry = new JobTelemetry { Type = JobTelemetryType.General, Message = $"TemplateEvaluatorComparisonError: EvaluateStepEnvironment: {ex.Message}" };
                    _context.Global.JobTelemetry.Add(telemetry);
                }
            }

            return legacyResult;
        }

        public Boolean EvaluateStepIf(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions,
            IEnumerable<KeyValuePair<String, Object>> expressionState)
        {
            var legacyResult = _legacyEvaluator.EvaluateStepIf(token, contextData, expressionFunctions, expressionState);

            if (_compare)
            {
                try
                {
                    _context.Debug("Comparing new template evaluator: EvaluateStepIf");
                    var convertedToken = ConvertToken(token);
                    var convertedData = ConvertData(contextData);
                    var convertedFunctions = ConvertFunctions(expressionFunctions);
                    var newResult = _newEvaluator.EvaluateStepIf(convertedToken, convertedData, convertedFunctions, expressionState);
                    if (legacyResult != newResult)
                    {
                        _context.Debug($"Mismatch: EvaluateStepIf differs (legacy={legacyResult}, new={newResult})");
                        var telemetry = new JobTelemetry { Type = JobTelemetryType.General, Message = "TemplateEvaluatorMismatch: EvaluateStepIf" };
                        _context.Global.JobTelemetry.Add(telemetry);
                    }
                }
                catch (Exception ex)
                {
                    _context.Debug($"Template evaluator comparison failed: {ex.Message}");
                    var telemetry = new JobTelemetry { Type = JobTelemetryType.General, Message = $"TemplateEvaluatorComparisonError: EvaluateStepIf: {ex.Message}" };
                    _context.Global.JobTelemetry.Add(telemetry);
                }
            }

            return legacyResult;
        }

        public Dictionary<String, String> EvaluateStepInputs(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            return _legacyEvaluator.EvaluateStepInputs(token, contextData, expressionFunctions);
        }

        public Int32 EvaluateStepTimeout(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            return _legacyEvaluator.EvaluateStepTimeout(token, contextData, expressionFunctions);
        }

        public GitHub.DistributedTask.Pipelines.JobContainer EvaluateJobContainer(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var legacyResult = _legacyEvaluator.EvaluateJobContainer(token, contextData, expressionFunctions);

            if (_compare)
            {
                try
                {
                    _context.Debug("Comparing new template evaluator: EvaluateJobContainer");
                    var convertedToken = ConvertToken(token);
                    var convertedData = ConvertData(contextData);
                    var convertedFunctions = ConvertFunctions(expressionFunctions);
                    var newResult = _newEvaluator.EvaluateJobContainer(convertedToken, convertedData, convertedFunctions);
                    if (!CompareJobContainer(legacyResult, newResult))
                    {
                        var telemetry = new JobTelemetry { Type = JobTelemetryType.General, Message = "TemplateEvaluatorMismatch: EvaluateJobContainer" };
                        _context.Global.JobTelemetry.Add(telemetry);
                    }
                }
                catch (Exception ex)
                {
                    _context.Debug($"Template evaluator comparison failed: {ex.Message}");
                    var telemetry = new JobTelemetry { Type = JobTelemetryType.General, Message = $"TemplateEvaluatorComparisonError: EvaluateJobContainer: {ex.Message}" };
                    _context.Global.JobTelemetry.Add(telemetry);
                }
            }

            return legacyResult;
        }

        public Dictionary<String, String> EvaluateJobOutput(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            return _legacyEvaluator.EvaluateJobOutput(token, contextData, expressionFunctions);
        }

        public TemplateToken EvaluateEnvironmentUrl(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            return _legacyEvaluator.EvaluateEnvironmentUrl(token, contextData, expressionFunctions);
        }

        public Dictionary<String, String> EvaluateJobDefaultsRun(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            return _legacyEvaluator.EvaluateJobDefaultsRun(token, contextData, expressionFunctions);
        }

        public IList<KeyValuePair<String, GitHub.DistributedTask.Pipelines.JobContainer>> EvaluateJobServiceContainers(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var legacyResult = _legacyEvaluator.EvaluateJobServiceContainers(token, contextData, expressionFunctions);

            if (_compare)
            {
                try
                {
                    _context.Debug("Comparing new template evaluator: EvaluateJobServiceContainers");
                    var convertedToken = ConvertToken(token);
                    var convertedData = ConvertData(contextData);
                    var convertedFunctions = ConvertFunctions(expressionFunctions);
                    var newResult = _newEvaluator.EvaluateJobServiceContainers(convertedToken, convertedData, convertedFunctions);
                    if (!CompareJobServiceContainers(legacyResult, newResult))
                    {
                        var telemetry = new JobTelemetry { Type = JobTelemetryType.General, Message = "TemplateEvaluatorMismatch: EvaluateJobServiceContainers" };
                        _context.Global.JobTelemetry.Add(telemetry);
                    }
                }
                catch (Exception ex)
                {
                    _context.Debug($"Template evaluator comparison failed: {ex.Message}");
                    var telemetry = new JobTelemetry { Type = JobTelemetryType.General, Message = $"TemplateEvaluatorComparisonError: EvaluateJobServiceContainers: {ex.Message}" };
                    _context.Global.JobTelemetry.Add(telemetry);
                }
            }

            return legacyResult;
        }

        public GitHub.DistributedTask.Pipelines.Snapshot EvaluateJobSnapshotRequest(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            return _legacyEvaluator.EvaluateJobSnapshotRequest(token, contextData, expressionFunctions);
        }

        private GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens.TemplateToken ConvertToken(
            GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateToken token)
        {
            if (token == null)
            {
                return null;
            }

            var json = StringUtil.ConvertToJson(token, Newtonsoft.Json.Formatting.None);
            return StringUtil.ConvertFromJson<GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens.TemplateToken>(json);
        }

        private GitHub.Actions.Expressions.Data.DictionaryExpressionData ConvertData(
            GitHub.DistributedTask.Pipelines.ContextData.DictionaryContextData contextData)
        {
            if (contextData == null)
            {
                return null;
            }

            var json = StringUtil.ConvertToJson(contextData, Newtonsoft.Json.Formatting.None);
            return StringUtil.ConvertFromJson<GitHub.Actions.Expressions.Data.DictionaryExpressionData>(json);
        }

        private IList<GitHub.Actions.Expressions.IFunctionInfo> ConvertFunctions(
            IList<GitHub.DistributedTask.Expressions2.IFunctionInfo> expressionFunctions)
        {
            if (expressionFunctions == null)
            {
                return null;
            }

            var result = new List<GitHub.Actions.Expressions.IFunctionInfo>();
            foreach (var func in expressionFunctions)
            {
                GitHub.Actions.Expressions.IFunctionInfo newFunc = func.Name switch
                {
                    "always" => new GitHub.Actions.Expressions.FunctionInfo<Expressions.NewAlwaysFunction>(func.Name, func.MinParameters, func.MaxParameters),
                    "cancelled" => new GitHub.Actions.Expressions.FunctionInfo<Expressions.NewCancelledFunction>(func.Name, func.MinParameters, func.MaxParameters),
                    "failure" => new GitHub.Actions.Expressions.FunctionInfo<Expressions.NewFailureFunction>(func.Name, func.MinParameters, func.MaxParameters),
                    "success" => new GitHub.Actions.Expressions.FunctionInfo<Expressions.NewSuccessFunction>(func.Name, func.MinParameters, func.MaxParameters),
                    "hashFiles" => new GitHub.Actions.Expressions.FunctionInfo<Expressions.NewHashFilesFunction>(func.Name, func.MinParameters, func.MaxParameters),
                    _ => throw new NotSupportedException($"Expression function '{func.Name}' is not supported for conversion")
                };
                result.Add(newFunc);
            }
            return result;
        }

        private bool CompareStepEnvironment(
            Dictionary<String, String> legacyResult,
            Dictionary<String, String> newResult)
        {
            return CompareDictionaries(legacyResult, newResult, "StepEnvironment");
        }

        private bool CompareJobContainer(
            GitHub.DistributedTask.Pipelines.JobContainer legacyResult,
            GitHub.Actions.WorkflowParser.JobContainer newResult)
        {
            if (legacyResult == null && newResult == null)
            {
                return true;
            }

            if (legacyResult == null || newResult == null)
            {
                _context.Debug($"Mismatch: one result is null (legacy={legacyResult == null}, new={newResult == null})");
                return false;
            }

            if (!string.Equals(legacyResult.Image, newResult.Image, StringComparison.Ordinal))
            {
                _context.Debug($"Mismatch: Image differs (legacy='{legacyResult.Image}', new='{newResult.Image}')");
                return false;
            }

            if (!string.Equals(legacyResult.Options, newResult.Options, StringComparison.Ordinal))
            {
                _context.Debug($"Mismatch: Options differs (legacy='{legacyResult.Options}', new='{newResult.Options}')");
                return false;
            }

            if (!CompareDictionaries(legacyResult.Environment, newResult.Environment, "Environment"))
            {
                return false;
            }

            if (!CompareLists(legacyResult.Volumes, newResult.Volumes, "Volumes"))
            {
                return false;
            }

            if (!CompareLists(legacyResult.Ports, newResult.Ports, "Ports"))
            {
                return false;
            }

            if (!CompareCredentials(legacyResult.Credentials, newResult.Credentials))
            {
                return false;
            }

            return true;
        }

        private bool CompareCredentials(
            GitHub.DistributedTask.Pipelines.ContainerRegistryCredentials legacyCreds,
            GitHub.Actions.WorkflowParser.ContainerRegistryCredentials newCreds)
        {
            if (legacyCreds == null && newCreds == null)
            {
                return true;
            }

            if (legacyCreds == null || newCreds == null)
            {
                _context.Debug($"Mismatch: Credentials - one is null (legacy={legacyCreds == null}, new={newCreds == null})");
                return false;
            }

            if (!string.Equals(legacyCreds.Username, newCreds.Username, StringComparison.Ordinal))
            {
                _context.Debug($"Mismatch: Credentials.Username differs (legacy='{legacyCreds.Username}', new='{newCreds.Username}')");
                return false;
            }

            if (!string.Equals(legacyCreds.Password, newCreds.Password, StringComparison.Ordinal))
            {
                _context.Debug($"Mismatch: Credentials.Password differs");
                return false;
            }

            return true;
        }

        private bool CompareLists(IList<String> legacyList, IList<String> newList, string fieldName)
        {
            if (legacyList == null && newList == null)
            {
                return true;
            }

            if (legacyList == null || newList == null)
            {
                _context.Debug($"Mismatch: {fieldName} - one is null (legacy={legacyList == null}, new={newList == null})");
                return false;
            }

            if (legacyList.Count != newList.Count)
            {
                _context.Debug($"Mismatch: {fieldName}.Count differs (legacy={legacyList.Count}, new={newList.Count})");
                return false;
            }

            for (int i = 0; i < legacyList.Count; i++)
            {
                if (!string.Equals(legacyList[i], newList[i], StringComparison.Ordinal))
                {
                    _context.Debug($"Mismatch: {fieldName}[{i}] differs (legacy='{legacyList[i]}', new='{newList[i]}')");
                    return false;
                }
            }

            return true;
        }

        private bool CompareDictionaries(IDictionary<String, String> legacyDict, IDictionary<String, String> newDict, string fieldName)
        {
            if (legacyDict == null && newDict == null)
            {
                return true;
            }

            if (legacyDict == null || newDict == null)
            {
                _context.Debug($"Mismatch: {fieldName} - one is null (legacy={legacyDict == null}, new={newDict == null})");
                return false;
            }

            if (legacyDict is Dictionary<String, String> legacyTypedDict && newDict is Dictionary<String, String> newTypedDict)
            {
                if (!object.Equals(legacyTypedDict.Comparer, newTypedDict.Comparer))
                {
                    _context.Debug($"Mismatch: {fieldName} - different comparers (legacy={legacyTypedDict.Comparer.GetType().Name}, new={newTypedDict.Comparer.GetType().Name})");
                    return false;
                }
            }

            if (legacyDict.Count != newDict.Count)
            {
                _context.Debug($"Mismatch: {fieldName}.Count differs (legacy={legacyDict.Count}, new={newDict.Count})");
                return false;
            }

            foreach (var kvp in legacyDict)
            {
                if (!newDict.TryGetValue(kvp.Key, out var newValue))
                {
                    _context.Debug($"Mismatch: {fieldName} - key '{kvp.Key}' missing in new result");
                    return false;
                }

                if (!string.Equals(kvp.Value, newValue, StringComparison.Ordinal))
                {
                    _context.Debug($"Mismatch: {fieldName}['{kvp.Key}'] differs (legacy='{kvp.Value}', new='{newValue}')");
                    return false;
                }
            }

            return true;
        }

        private bool CompareJobServiceContainers(
            IList<KeyValuePair<String, GitHub.DistributedTask.Pipelines.JobContainer>> legacyResult,
            IList<KeyValuePair<String, GitHub.Actions.WorkflowParser.JobContainer>> newResult)
        {
            if (legacyResult == null && newResult == null)
            {
                return true;
            }

            if (legacyResult == null || newResult == null)
            {
                _context.Debug($"Mismatch: one result is null (legacy={legacyResult == null}, new={newResult == null})");
                return false;
            }

            if (legacyResult.Count != newResult.Count)
            {
                _context.Debug($"Mismatch: ServiceContainers.Count differs (legacy={legacyResult.Count}, new={newResult.Count})");
                return false;
            }

            for (int i = 0; i < legacyResult.Count; i++)
            {
                var legacyKvp = legacyResult[i];
                var newKvp = newResult[i];

                if (!string.Equals(legacyKvp.Key, newKvp.Key, StringComparison.Ordinal))
                {
                    _context.Debug($"Mismatch: ServiceContainers[{i}].Key differs (legacy='{legacyKvp.Key}', new='{newKvp.Key}')");
                    return false;
                }

                if (!CompareJobContainer(legacyKvp.Value, newKvp.Value))
                {
                    _context.Debug($"Mismatch: ServiceContainers['{legacyKvp.Key}']");
                    return false;
                }
            }

            return true;
        }
    }
}

