using System;
using System.Collections.Generic;
using System.ComponentModel;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Logging;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides the environment and services available during build and execution of a pipeline. 
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IPipelineContext
    {
        DictionaryContextData Data { get; }

        Int32 EnvironmentVersion { get; }

        EvaluationOptions ExpressionOptions { get; }

        ISecretMasker SecretMasker { get; }

        IPipelineTraceWriter Trace { get; }

        ISet<String> SystemVariableNames { get; }

        IDictionary<String, VariableValue> Variables { get; }

        String ExpandVariables(String value, Boolean maskSecrets = false);

        ExpressionResult<T> Evaluate<T>(String expression);

        ExpressionResult<JObject> Evaluate(JObject value);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IPipelineTraceWriter : ITraceWriter
    {
        void EnterProperty(String name);
        void LeaveProperty(String name);
    }
}
