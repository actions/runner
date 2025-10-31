using System;
using System.Collections.Generic;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;

namespace GitHub.DistributedTask.Pipelines.ObjectTemplating
{
    /// <summary>
    /// Evaluates parts of the workflow DOM. For example, a job strategy or step inputs.
    /// </summary>
    public interface IPipelineTemplateEvaluator
    {
        Boolean EvaluateStepContinueOnError(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions);

        String EvaluateStepDisplayName(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions);

        Dictionary<String, String> EvaluateStepEnvironment(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions,
            StringComparer keyComparer);

        Boolean EvaluateStepIf(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions,
            IEnumerable<KeyValuePair<String, Object>> expressionState);

        Dictionary<String, String> EvaluateStepInputs(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions);

        Int32 EvaluateStepTimeout(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions);

        JobContainer EvaluateJobContainer(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions);

        Dictionary<String, String> EvaluateJobOutput(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions);

        TemplateToken EvaluateEnvironmentUrl(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions);

        Dictionary<String, String> EvaluateJobDefaultsRun(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions);

        IList<KeyValuePair<String, JobContainer>> EvaluateJobServiceContainers(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions);

        Snapshot EvaluateJobSnapshotRequest(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions);
    }
}
