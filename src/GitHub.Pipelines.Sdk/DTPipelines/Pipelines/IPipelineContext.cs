using System;
using System.Collections.Generic;
using System.ComponentModel;
using GitHub.DistributedTask.Expressions;
using GitHub.DistributedTask.Logging;
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
        ICounterStore CounterStore { get; }

        Int32 EnvironmentVersion { get; }

        EvaluationOptions ExpressionOptions { get; }

        IPipelineIdGenerator IdGenerator { get; }

        IPackageStore PackageStore { get; }

        PipelineResources ReferencedResources { get; }

        IResourceStore ResourceStore { get; }

        IReadOnlyList<IStepProvider> StepProviders { get; }

        ISecretMasker SecretMasker { get; }

        ITaskStore TaskStore { get; }

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
