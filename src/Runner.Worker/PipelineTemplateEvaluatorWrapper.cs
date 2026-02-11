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
        private PipelineTemplateEvaluator _legacyEvaluator;
        private WorkflowTemplateEvaluator _newEvaluator;
        private IExecutionContext _context;
        private Tracing _trace;

        public PipelineTemplateEvaluatorWrapper(
            IHostContext hostContext,
            IExecutionContext context,
            ObjectTemplating.ITraceWriter traceWriter = null)
        {
            ArgUtil.NotNull(hostContext, nameof(hostContext));
            ArgUtil.NotNull(context, nameof(context));
            _context = context;
            _trace = hostContext.GetTrace(nameof(PipelineTemplateEvaluatorWrapper));

            if (traceWriter == null)
            {
                traceWriter = context.ToTemplateTraceWriter();
            }

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

        public bool EvaluateStepContinueOnError(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            return EvaluateAndCompare(
                "EvaluateStepContinueOnError",
                () => _legacyEvaluator.EvaluateStepContinueOnError(token, contextData, expressionFunctions),
                () => _newEvaluator.EvaluateStepContinueOnError(ConvertToken(token), ConvertData(contextData), ConvertFunctions(expressionFunctions)),
                (legacyResult, newResult) => legacyResult == newResult);
        }

        public string EvaluateStepDisplayName(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            return EvaluateAndCompare(
                "EvaluateStepDisplayName",
                () => _legacyEvaluator.EvaluateStepDisplayName(token, contextData, expressionFunctions),
                () => _newEvaluator.EvaluateStepName(ConvertToken(token), ConvertData(contextData), ConvertFunctions(expressionFunctions)),
                (legacyResult, newResult) => string.Equals(legacyResult, newResult, StringComparison.Ordinal));
        }

        public Dictionary<string, string> EvaluateStepEnvironment(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions,
            StringComparer keyComparer)
        {
            return EvaluateAndCompare(
                "EvaluateStepEnvironment",
                () => _legacyEvaluator.EvaluateStepEnvironment(token, contextData, expressionFunctions, keyComparer),
                () => _newEvaluator.EvaluateStepEnvironment(ConvertToken(token), ConvertData(contextData), ConvertFunctions(expressionFunctions), keyComparer),
                CompareStepEnvironment);
        }

        public bool EvaluateStepIf(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions,
            IEnumerable<KeyValuePair<string, object>> expressionState)
        {
            return EvaluateAndCompare(
                "EvaluateStepIf",
                () => _legacyEvaluator.EvaluateStepIf(token, contextData, expressionFunctions, expressionState),
                () => _newEvaluator.EvaluateStepIf(ConvertToken(token), ConvertData(contextData), ConvertFunctions(expressionFunctions), expressionState),
                (legacyResult, newResult) => legacyResult == newResult);
        }

        public Dictionary<string, string> EvaluateStepInputs(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            return EvaluateAndCompare(
                "EvaluateStepInputs",
                () => _legacyEvaluator.EvaluateStepInputs(token, contextData, expressionFunctions),
                () => _newEvaluator.EvaluateStepInputs(ConvertToken(token), ConvertData(contextData), ConvertFunctions(expressionFunctions)),
                (legacyResult, newResult) => CompareDictionaries(legacyResult, newResult, "StepInputs"));
        }

        public int EvaluateStepTimeout(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            return EvaluateAndCompare(
                "EvaluateStepTimeout",
                () => _legacyEvaluator.EvaluateStepTimeout(token, contextData, expressionFunctions),
                () => _newEvaluator.EvaluateStepTimeout(ConvertToken(token), ConvertData(contextData), ConvertFunctions(expressionFunctions)),
                (legacyResult, newResult) => legacyResult == newResult);
        }

        public GitHub.DistributedTask.Pipelines.JobContainer EvaluateJobContainer(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            return EvaluateAndCompare(
                "EvaluateJobContainer",
                () => _legacyEvaluator.EvaluateJobContainer(token, contextData, expressionFunctions),
                () => _newEvaluator.EvaluateJobContainer(ConvertToken(token), ConvertData(contextData), ConvertFunctions(expressionFunctions)),
                CompareJobContainer);
        }

        public Dictionary<string, string> EvaluateJobOutput(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            return EvaluateAndCompare(
                "EvaluateJobOutput",
                () => _legacyEvaluator.EvaluateJobOutput(token, contextData, expressionFunctions),
                () => _newEvaluator.EvaluateJobOutputs(ConvertToken(token), ConvertData(contextData), ConvertFunctions(expressionFunctions)),
                (legacyResult, newResult) => CompareDictionaries(legacyResult, newResult, "JobOutput"));
        }

        public TemplateToken EvaluateEnvironmentUrl(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            return EvaluateAndCompare(
                "EvaluateEnvironmentUrl",
                () => _legacyEvaluator.EvaluateEnvironmentUrl(token, contextData, expressionFunctions),
                () => _newEvaluator.EvaluateJobEnvironmentUrl(ConvertToken(token), ConvertData(contextData), ConvertFunctions(expressionFunctions)),
                CompareEnvironmentUrl);
        }

        public Dictionary<string, string> EvaluateJobDefaultsRun(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            return EvaluateAndCompare(
                "EvaluateJobDefaultsRun",
                () => _legacyEvaluator.EvaluateJobDefaultsRun(token, contextData, expressionFunctions),
                () => _newEvaluator.EvaluateJobDefaultsRun(ConvertToken(token), ConvertData(contextData), ConvertFunctions(expressionFunctions)),
                (legacyResult, newResult) => CompareDictionaries(legacyResult, newResult, "JobDefaultsRun"));
        }

        public IList<KeyValuePair<string, GitHub.DistributedTask.Pipelines.JobContainer>> EvaluateJobServiceContainers(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            return EvaluateAndCompare(
                "EvaluateJobServiceContainers",
                () => _legacyEvaluator.EvaluateJobServiceContainers(token, contextData, expressionFunctions),
                () => _newEvaluator.EvaluateJobServiceContainers(ConvertToken(token), ConvertData(contextData), ConvertFunctions(expressionFunctions)),
                (legacyResult, newResult) => CompareJobServiceContainers(legacyResult, newResult));
        }

        public GitHub.DistributedTask.Pipelines.Snapshot EvaluateJobSnapshotRequest(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            return EvaluateAndCompare(
                "EvaluateJobSnapshotRequest",
                () => _legacyEvaluator.EvaluateJobSnapshotRequest(token, contextData, expressionFunctions),
                () => _newEvaluator.EvaluateSnapshot(string.Empty, ConvertToken(token), ConvertData(contextData), ConvertFunctions(expressionFunctions)),
                CompareSnapshot);
        }

        private void RecordMismatch(string methodName)
        {
            if (!_context.Global.HasTemplateEvaluatorMismatch)
            {
                _context.Global.HasTemplateEvaluatorMismatch = true;
                var telemetry = new JobTelemetry { Type = JobTelemetryType.General, Message = $"TemplateEvaluatorMismatch: {methodName}" };
                _context.Global.JobTelemetry.Add(telemetry);
            }
        }

        private void RecordComparisonError(string errorDetails)
        {
            if (!_context.Global.HasTemplateEvaluatorMismatch)
            {
                _context.Global.HasTemplateEvaluatorMismatch = true;
                var telemetry = new JobTelemetry { Type = JobTelemetryType.General, Message = $"TemplateEvaluatorComparisonError: {errorDetails}" };
                _context.Global.JobTelemetry.Add(telemetry);
            }
        }

        internal TLegacy EvaluateAndCompare<TLegacy, TNew>(
            string methodName,
            Func<TLegacy> legacyEvaluator,
            Func<TNew> newEvaluator,
            Func<TLegacy, TNew, bool> resultComparer)
        {
            // Capture cancellation state before evaluation
            var cancellationRequestedBefore = _context.CancellationToken.IsCancellationRequested;

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
                ArgUtil.NotNull(_context, nameof(_context));
                ArgUtil.NotNull(_newEvaluator, nameof(_newEvaluator));
                _trace.Info(methodName);

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

                // Capture cancellation state after evaluation
                var cancellationRequestedAfter = _context.CancellationToken.IsCancellationRequested;

                // Compare results or exceptions
                bool hasMismatch = false;
                if (legacyException != null || newException != null)
                {
                    // Either one or both threw exceptions - compare them
                    if (!CompareExceptions(legacyException, newException))
                    {
                        _trace.Info($"{methodName} exception mismatch");
                        hasMismatch = true;
                    }
                }
                else
                {
                    // Both succeeded - compare results
                    if (!resultComparer(legacyResult, newResult))
                    {
                        _trace.Info($"{methodName} mismatch");
                        hasMismatch = true;
                    }
                }

                // Only record mismatch if it wasn't caused by a cancellation race condition
                if (hasMismatch)
                {
                    if (!cancellationRequestedBefore && cancellationRequestedAfter)
                    {
                        // Cancellation state changed during evaluation window - skip recording
                        _trace.Info($"{methodName} mismatch skipped due to cancellation race condition");
                    }
                    else
                    {
                        RecordMismatch($"{methodName}");
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.Info($"Comparison failed: {ex.Message}");
                RecordComparisonError($"{methodName}: {ex.Message}");
            }

            // Re-throw legacy exception if any
            if (legacyException != null)
            {
                throw legacyException;
            }

            return legacyResult;
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
            Dictionary<string, string> legacyResult,
            Dictionary<string, string> newResult)
        {
            return CompareDictionaries(legacyResult, newResult, "StepEnvironment");
        }

        private bool CompareEnvironmentUrl(
            TemplateToken legacyResult,
            GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens.TemplateToken newResult)
        {
            var legacyJson = legacyResult != null ? Newtonsoft.Json.JsonConvert.SerializeObject(legacyResult, Newtonsoft.Json.Formatting.None) : null;
            var newJson = newResult != null ? Newtonsoft.Json.JsonConvert.SerializeObject(newResult, Newtonsoft.Json.Formatting.None) : null;
            return legacyJson == newJson;
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
                _trace.Info($"CompareJobContainer mismatch - one result is null (legacy={legacyResult == null}, new={newResult == null})");
                return false;
            }

            if (!string.Equals(legacyResult.Image, newResult.Image, StringComparison.Ordinal))
            {
                _trace.Info($"CompareJobContainer mismatch - Image differs (legacy='{legacyResult.Image}', new='{newResult.Image}')");
                return false;
            }

            if (!string.Equals(legacyResult.Options, newResult.Options, StringComparison.Ordinal))
            {
                _trace.Info($"CompareJobContainer mismatch - Options differs (legacy='{legacyResult.Options}', new='{newResult.Options}')");
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
                _trace.Info($"CompareCredentials mismatch - one is null (legacy={legacyCreds == null}, new={newCreds == null})");
                return false;
            }

            if (!string.Equals(legacyCreds.Username, newCreds.Username, StringComparison.Ordinal))
            {
                _trace.Info($"CompareCredentials mismatch - Credentials.Username differs (legacy='{legacyCreds.Username}', new='{newCreds.Username}')");
                return false;
            }

            if (!string.Equals(legacyCreds.Password, newCreds.Password, StringComparison.Ordinal))
            {
                _trace.Info($"CompareCredentials mismatch - Credentials.Password differs");
                return false;
            }

            return true;
        }

        private bool CompareLists(IList<string> legacyList, IList<string> newList, string fieldName)
        {
            if (legacyList == null && newList == null)
            {
                return true;
            }

            if (legacyList == null || newList == null)
            {
                _trace.Info($"CompareLists mismatch - {fieldName} - one is null (legacy={legacyList == null}, new={newList == null})");
                return false;
            }

            if (legacyList.Count != newList.Count)
            {
                _trace.Info($"CompareLists mismatch - {fieldName}.Count differs (legacy={legacyList.Count}, new={newList.Count})");
                return false;
            }

            for (int i = 0; i < legacyList.Count; i++)
            {
                if (!string.Equals(legacyList[i], newList[i], StringComparison.Ordinal))
                {
                    _trace.Info($"CompareLists mismatch - {fieldName}[{i}] differs (legacy='{legacyList[i]}', new='{newList[i]}')");
                    return false;
                }
            }

            return true;
        }

        private bool CompareDictionaries(IDictionary<string, string> legacyDict, IDictionary<string, string> newDict, string fieldName)
        {
            if (legacyDict == null && newDict == null)
            {
                return true;
            }

            if (legacyDict == null || newDict == null)
            {
                _trace.Info($"CompareDictionaries mismatch - {fieldName} - one is null (legacy={legacyDict == null}, new={newDict == null})");
                return false;
            }

            if (legacyDict is Dictionary<String, String> legacyTypedDict && newDict is Dictionary<String, String> newTypedDict)
            {
                if (!object.Equals(legacyTypedDict.Comparer, newTypedDict.Comparer))
                {
                    _trace.Info($"CompareDictionaries mismatch - {fieldName} - different comparers (legacy={legacyTypedDict.Comparer.GetType().Name}, new={newTypedDict.Comparer.GetType().Name})");
                    return false;
                }
            }

            if (legacyDict.Count != newDict.Count)
            {
                _trace.Info($"CompareDictionaries mismatch - {fieldName}.Count differs (legacy={legacyDict.Count}, new={newDict.Count})");
                return false;
            }

            foreach (var kvp in legacyDict)
            {
                if (!newDict.TryGetValue(kvp.Key, out var newValue))
                {
                    _trace.Info($"CompareDictionaries mismatch - {fieldName} - key '{kvp.Key}' missing in new result");
                    return false;
                }

                if (!string.Equals(kvp.Value, newValue, StringComparison.Ordinal))
                {
                    _trace.Info($"CompareDictionaries mismatch - {fieldName}['{kvp.Key}'] differs (legacy='{kvp.Value}', new='{newValue}')");
                    return false;
                }
            }

            return true;
        }

        private bool CompareJobServiceContainers(
            IList<KeyValuePair<string, GitHub.DistributedTask.Pipelines.JobContainer>> legacyResult,
            IList<KeyValuePair<string, GitHub.Actions.WorkflowParser.JobContainer>> newResult)
        {
            if (legacyResult == null && newResult == null)
            {
                return true;
            }

            if (legacyResult == null || newResult == null)
            {
                _trace.Info($"CompareJobServiceContainers mismatch - one result is null (legacy={legacyResult == null}, new={newResult == null})");
                return false;
            }

            if (legacyResult.Count != newResult.Count)
            {
                _trace.Info($"CompareJobServiceContainers mismatch - ServiceContainers.Count differs (legacy={legacyResult.Count}, new={newResult.Count})");
                return false;
            }

            for (int i = 0; i < legacyResult.Count; i++)
            {
                var legacyKvp = legacyResult[i];
                var newKvp = newResult[i];

                if (!string.Equals(legacyKvp.Key, newKvp.Key, StringComparison.Ordinal))
                {
                    _trace.Info($"CompareJobServiceContainers mismatch - ServiceContainers[{i}].Key differs (legacy='{legacyKvp.Key}', new='{newKvp.Key}')");
                    return false;
                }

                if (!CompareJobContainer(legacyKvp.Value, newKvp.Value))
                {
                    _trace.Info($"CompareJobServiceContainers mismatch - ServiceContainers['{legacyKvp.Key}']");
                    return false;
                }
            }

            return true;
        }

        private bool CompareSnapshot(
            GitHub.DistributedTask.Pipelines.Snapshot legacyResult,
            GitHub.Actions.WorkflowParser.Snapshot newResult)
        {
            if (legacyResult == null && newResult == null)
            {
                return true;
            }

            if (legacyResult == null || newResult == null)
            {
                _trace.Info($"CompareSnapshot mismatch - one is null (legacy={legacyResult == null}, new={newResult == null})");
                return false;
            }

            if (!string.Equals(legacyResult.ImageName, newResult.ImageName, StringComparison.Ordinal))
            {
                _trace.Info($"CompareSnapshot mismatch - Snapshot.ImageName differs (legacy='{legacyResult.ImageName}', new='{newResult.ImageName}')");
                return false;
            }

            if (!string.Equals(legacyResult.Version, newResult.Version, StringComparison.Ordinal))
            {
                _trace.Info($"CompareSnapshot mismatch - Snapshot.Version differs (legacy='{legacyResult.Version}', new='{newResult.Version}')");
                return false;
            }

            // Compare Condition (legacy) vs If (new)
            // Legacy has Condition as string, new has If as BasicExpressionToken
            // For comparison, we'll serialize the If token and compare with Condition
            var newIfValue = newResult.If != null ? Newtonsoft.Json.JsonConvert.SerializeObject(newResult.If, Newtonsoft.Json.Formatting.None) : null;

            // Legacy Condition is a string expression like "success()"
            // New If is a BasicExpressionToken that needs to be serialized
            // We'll do a basic comparison - if both are null/empty or both exist
            var legacyHasCondition = !string.IsNullOrEmpty(legacyResult.Condition);
            var newHasIf = newResult.If != null;

            if (legacyHasCondition != newHasIf)
            {
                _trace.Info($"CompareSnapshot mismatch - condition/if presence differs (legacy has condition={legacyHasCondition}, new has if={newHasIf})");
                return false;
            }

            return true;
        }

        private bool CompareExceptions(Exception legacyException, Exception newException)
        {
            if (legacyException == null && newException == null)
            {
                return true;
            }

            if (legacyException == null || newException == null)
            {
                _trace.Info($"CompareExceptions mismatch - one exception is null (legacy={legacyException == null}, new={newException == null})");
                return false;
            }

            // Check for known equivalent error patterns (e.g., JSON parse errors)
            // where both parsers correctly reject invalid input but with different wording
            if (IsKnownEquivalentErrorPattern(legacyException, newException))
            {
                return true;
            }

            // Compare exception messages recursively (including inner exceptions)
            var legacyMessages = GetExceptionMessages(legacyException);
            var newMessages = GetExceptionMessages(newException);

            if (legacyMessages.Count != newMessages.Count)
            {
                _trace.Info($"CompareExceptions mismatch - different number of exception messages (legacy={legacyMessages.Count}, new={newMessages.Count})");
                return false;
            }

            for (int i = 0; i < legacyMessages.Count; i++)
            {
                if (!string.Equals(legacyMessages[i], newMessages[i], StringComparison.Ordinal))
                {
                    _trace.Info($"CompareExceptions mismatch - exception messages differ at level {i} (legacy='{legacyMessages[i]}', new='{newMessages[i]}')");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if two exceptions match a known pattern where both parsers correctly reject
        /// invalid input but with different error messages (e.g., JSON parse errors from fromJSON).
        /// </summary>
        private bool IsKnownEquivalentErrorPattern(Exception legacyException, Exception newException)
        {
            // fromJSON('') - both parsers fail when parsing empty string as JSON
            // The error messages differ but both indicate JSON parsing failure.
            // Legacy throws raw JsonReaderException: "Error reading JToken from JsonReader..."
            // New wraps it: "Error parsing fromJson" with inner JsonReaderException
            // Both may be wrapped in TemplateValidationException: "The template is not valid..."
            if (HasJsonExceptionType(legacyException) && HasJsonExceptionType(newException))
            {
                _trace.Info("CompareExceptions - both exceptions are JSON parse errors, treating as matched");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the exception chain contains a JSON-related exception type.
        /// </summary>
        internal static bool HasJsonExceptionType(Exception ex)
        {
            var toProcess = new Queue<Exception>();
            toProcess.Enqueue(ex);
            int count = 0;

            while (toProcess.Count > 0 && count < 50)
            {
                var current = toProcess.Dequeue();
                if (current == null) continue;

                count++;

                if (current is Newtonsoft.Json.JsonReaderException ||
                    current is System.Text.Json.JsonException)
                {
                    return true;
                }

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
            }

            return false;
        }

        private IList<string> GetExceptionMessages(Exception ex)
        {
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
                    _trace.Info("CompareExceptions failsafe triggered - too many exceptions (50+)");
                    break;
                }
            }

            return messages;
        }
    }
}
