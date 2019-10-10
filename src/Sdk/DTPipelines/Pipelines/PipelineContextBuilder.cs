using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.Runtime;
using GitHub.DistributedTask.Pipelines.Validation;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Common;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal sealed class PipelineContextBuilder
    {
        public PipelineContextBuilder()
            : this(null, null, null, null, null)
        {
        }

        public PipelineContextBuilder(
            ICounterStore counterStore = null,
            IPackageStore packageStore = null,
            IResourceStore resourceStore = null,
            IList<IStepProvider> stepProviders = null,
            ITaskStore taskStore = null,
            IPipelineIdGenerator idGenerator = null,
            EvaluationOptions expressionOptions = null,
            IList<IPhaseProvider> phaseProviders = null)
        {
            this.EnvironmentVersion = 2;
            this.CounterStore = counterStore ?? new CounterStore();
            this.IdGenerator = idGenerator ?? new PipelineIdGenerator();
            this.PackageStore = packageStore ?? new PackageStore();
            this.ResourceStore = resourceStore ?? new ResourceStore();
            this.StepProviders = stepProviders ?? new List<IStepProvider>();
            this.TaskStore = taskStore ?? new TaskStore();
            this.ExpressionOptions = expressionOptions ?? new EvaluationOptions();
            this.PhaseProviders = phaseProviders ?? new List<IPhaseProvider>();
        }

        internal PipelineContextBuilder(IPipelineContext context)
            : this(context.CounterStore, context.PackageStore, context.ResourceStore, context.StepProviders.ToList(), context.TaskStore, context.IdGenerator, context.ExpressionOptions)
        {
            m_context = context;

            var userVariables = new List<IVariable>();
            var systemVariables = new VariablesDictionary();
            foreach (var variable in context.Variables)
            {
                if (context.SystemVariableNames.Contains(variable.Key))
                {
                    systemVariables[variable.Key] = variable.Value.Clone();
                }
                else
                {
                    var userVariable = new Variable
                    {
                        Name = variable.Key,
                        Secret = variable.Value?.IsSecret ?? false,
                        Value = variable.Value?.Value,
                    };

                    userVariables.Add(userVariable);
                }
            }

            // For simplicity the variables are currently marked as read-only for the explicit
            // context scenario.
            m_userVariables = userVariables.AsReadOnly();
            m_systemVariables = systemVariables.AsReadOnly();
        }

        /// <summary>
        /// Gets the counter store configured for the builder.
        /// </summary>
        public ICounterStore CounterStore
        {
            get;
        }

        public IEnvironmentStore EnvironmentStore
        {
            get;
        }

        /// <summary>
        /// Gets or sets the environment version, controlling behaviors related to variable groups and step injection.
        /// </summary>
        public Int32 EnvironmentVersion
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the generator for pipeline identifiers.
        /// </summary>
        public IPipelineIdGenerator IdGenerator
        {
            get;
        }

        /// <summary>
        /// Gets the package store configured for the builder.
        /// </summary>
        public IPackageStore PackageStore
        {
            get;
        }

        /// <summary>
        /// Gets the resource store configured for the builder.
        /// </summary>
        public IResourceStore ResourceStore
        {
            get;
        }

        /// <summary>
        /// Gets the list of step providers configured for the builder.
        /// </summary>
        public IList<IStepProvider> StepProviders
        {
            get;
        }

        public IList<IPhaseProvider> PhaseProviders
        {
            get;
        }

        /// <summary>
        /// Gets the task store configured for the builder.
        /// </summary>
        public ITaskStore TaskStore
        {
            get;
        }

        /// <summary>
        /// Gets the expression evaluation options configured for the builder.
        /// </summary>
        public EvaluationOptions ExpressionOptions
        {
            get;
        }

        /// <summary>
        /// Gets a list of variable sets included in the pipeline.
        /// </summary>
        public IList<IVariable> UserVariables
        {
            get
            {
                if (m_userVariables == null)
                {
                    m_userVariables = new List<IVariable>();
                }
                return m_userVariables;
            }
        }

        /// <summary>
        /// Gets the system variables included in the pipeline.
        /// </summary>
        public IDictionary<String, VariableValue> SystemVariables
        {
            get
            {
                if (m_systemVariables == null)
                {
                    m_systemVariables = new VariablesDictionary();
                }
                return m_systemVariables;
            }
        }

        public PipelineBuildContext CreateBuildContext(
            BuildOptions options,
            IPackageStore packageStore = null,
            Boolean includeSecrets = false)
        {
            PipelineBuildContext context = null;
            if (m_context == null)
            {
                context = new PipelineBuildContext(options, null, this.CounterStore, this.ResourceStore, this.StepProviders, this.TaskStore, packageStore, new InputValidator(), null, this.ExpressionOptions, this.PhaseProviders);
                SetVariables(context, includeSecrets: includeSecrets);
                context.EnvironmentVersion = this.EnvironmentVersion;
            }
            else
            {
                context = new PipelineBuildContext(m_context, options);
            }

            return context;
        }

        public StageExecutionContext CreateStageExecutionContext(
            StageInstance stage,
            PipelineState state = PipelineState.InProgress,
            DictionaryContextData data = null,
            Boolean includeSecrets = false,
            IPipelineTraceWriter trace = null,
            ExecutionOptions executionOptions = null)
        {
            if (m_context != null)
            {
                throw new NotSupportedException();
            }

            var context = new StageExecutionContext(stage, state, data, this.CounterStore, this.PackageStore, this.ResourceStore, this.TaskStore, this.StepProviders, this.IdGenerator, trace, this.ExpressionOptions, executionOptions);
            SetVariables(context, stage, includeSecrets: includeSecrets);
            context.EnvironmentVersion = this.EnvironmentVersion;
            return context;
        }

        public PhaseExecutionContext CreatePhaseExecutionContext(
            StageInstance stage,
            PhaseInstance phase,
            PipelineState state = PipelineState.InProgress,
            DictionaryContextData data = null,
            Boolean includeSecrets = false,
            IPipelineTraceWriter trace = null,
            ExecutionOptions executionOptions = null)
        {
            if (m_context != null)
            {
                throw new NotSupportedException();
            }

            var context = new PhaseExecutionContext(stage, phase, state, data, this.CounterStore, this.PackageStore, this.ResourceStore, this.TaskStore, this.StepProviders, this.IdGenerator, trace, this.ExpressionOptions, executionOptions);
            SetVariables(context, stage, phase, includeSecrets);
            context.EnvironmentVersion = this.EnvironmentVersion;
            return context;
        }

        private void SetVariables(
            IPipelineContext context,
            StageInstance stage = null,
            PhaseInstance phase = null,
            Boolean includeSecrets = false)
        {
            // Next merge variables specified from alternative sources in the order they are presented. This may
            // be specified by build/release definitions or lower constructs, or may be specified by the user as
            // input variables to override other values.
            var referencedVariableGroups = new Dictionary<String, VariableGroup>(StringComparer.OrdinalIgnoreCase);
            var expressionsToEvaluate = new Dictionary<String, ExpressionValue<String>>(StringComparer.OrdinalIgnoreCase);
            if (m_userVariables?.Count > 0 || stage?.Definition?.Variables.Count > 0 || phase?.Definition?.Variables.Count > 0)
            {
                var userVariables = this.UserVariables.Concat(stage?.Definition?.Variables ?? Enumerable.Empty<IVariable>()).Concat(phase?.Definition?.Variables ?? Enumerable.Empty<IVariable>());
                foreach (var variable in userVariables)
                {
                    if (variable is Variable inlineVariable)
                    {
                        if (ExpressionValue.TryParse<String>(inlineVariable.Value, out var expression))
                        {
                            expressionsToEvaluate[inlineVariable.Name] = expression;
                        }

                        if (context.Variables.TryGetValue(inlineVariable.Name, out VariableValue existingValue))
                        {
                            existingValue.Value = inlineVariable.Value;
                            existingValue.IsSecret |= inlineVariable.Secret;

                            // Remove the reference to the variable group
                            referencedVariableGroups.Remove(inlineVariable.Name);
                        }
                        else
                        {
                            context.Variables[inlineVariable.Name] = new VariableValue(inlineVariable.Value, inlineVariable.Secret);
                        }
                    }
                    else if (variable is VariableGroupReference groupReference)
                    {
                        var variableGroup = this.ResourceStore.VariableGroups.Get(groupReference);
                        if (variableGroup == null)
                        {
                            throw new ResourceNotFoundException(PipelineStrings.VariableGroupNotFound(variableGroup));
                        }

                        // A pre-computed list of keys wins if it is present, otherwise we compute it dynamically
                        if (groupReference.SecretStore?.Keys.Count > 0)
                        {
                            foreach (var key in groupReference.SecretStore.Keys)
                            {
                                // Ignore the key if it isn't present in the variable group
                                if (!variableGroup.Variables.TryGetValue(key, out var variableGroupEntry))
                                {
                                    continue;
                                }

                                // Variable groups which have secrets providers use delayed resolution depending on targets
                                // being invoked, etc.
                                if (context.Variables.TryGetValue(key, out VariableValue existingValue))
                                {
                                    existingValue.Value = variableGroupEntry.Value;
                                    existingValue.IsSecret |= variableGroupEntry.IsSecret;
                                    referencedVariableGroups[key] = variableGroup;
                                }
                                else
                                {
                                    var clonedValue = variableGroupEntry.Clone();
                                    clonedValue.Value = variableGroupEntry.Value;
                                    context.Variables[key] = clonedValue;
                                    referencedVariableGroups[key] = variableGroup;
                                }
                            }
                        }
                        else
                        {
                            foreach (var variableGroupEntry in variableGroup.Variables.Where(v => v.Value != null))
                            {
                                // Variable groups which have secrets providers use delayed resolution depending on targets
                                // being invoked, etc.
                                if (context.Variables.TryGetValue(variableGroupEntry.Key, out VariableValue existingValue))
                                {
                                    existingValue.Value = variableGroupEntry.Value.Value;
                                    existingValue.IsSecret |= variableGroupEntry.Value.IsSecret;
                                    referencedVariableGroups[variableGroupEntry.Key] = variableGroup;
                                }
                                else
                                {
                                    var clonedValue = variableGroupEntry.Value.Clone();
                                    clonedValue.Value = variableGroupEntry.Value.Value;
                                    context.Variables[variableGroupEntry.Key] = clonedValue;
                                    referencedVariableGroups[variableGroupEntry.Key] = variableGroup;
                                }
                            }
                        }
                    }
                }
            }

            // System variables get applied last as they always win
            if (m_systemVariables?.Count > 0 || stage != null || phase != null)
            {
                // Start with system variables specified in the pipeline and then overlay scopes in order
                var systemVariables = m_systemVariables == null
                    ? new VariablesDictionary()
                    : new VariablesDictionary(m_systemVariables);

                // Setup stage variables
                if (stage != null)
                {
                    systemVariables[WellKnownDistributedTaskVariables.StageDisplayName] = stage.Definition?.DisplayName ?? stage.Name;
                    systemVariables[WellKnownDistributedTaskVariables.StageId] = this.IdGenerator.GetStageInstanceId(stage.Name, stage.Attempt).ToString("D");
                    systemVariables[WellKnownDistributedTaskVariables.StageName] = stage.Name;
                    systemVariables[WellKnownDistributedTaskVariables.StageAttempt] = stage.Attempt.ToString();
                }

                // Setup phase variables
                if (phase != null)
                {
                    systemVariables[WellKnownDistributedTaskVariables.PhaseDisplayName] = phase.Definition?.DisplayName ?? phase.Name;
                    systemVariables[WellKnownDistributedTaskVariables.PhaseId] = this.IdGenerator.GetPhaseInstanceId(stage?.Name, phase.Name, phase.Attempt).ToString("D");
                    systemVariables[WellKnownDistributedTaskVariables.PhaseName] = phase.Name;
                    systemVariables[WellKnownDistributedTaskVariables.PhaseAttempt] = phase.Attempt.ToString();
                }

                foreach (var systemVariable in systemVariables)
                {
                    referencedVariableGroups.Remove(systemVariable.Key);
                    context.Variables[systemVariable.Key] = systemVariable.Value?.Clone();

                    if (ExpressionValue.TryParse<String>(systemVariable.Value?.Value, out var expression))
                    {
                        expressionsToEvaluate[systemVariable.Key] = expression;
                    }

                    context.SystemVariableNames.Add(systemVariable.Key);
                }
            }

            if (referencedVariableGroups.Count > 0 || expressionsToEvaluate.Count > 0)
            {
                context.Trace?.EnterProperty("Variables");
            }

            // Now populate the environment with variable group resources which are needed for execution
            if (referencedVariableGroups.Count > 0)
            {
                foreach (var variableGroupData in referencedVariableGroups.GroupBy(x => x.Value, x => x.Key, s_comparer.Value))
                {
                    // If our variable group accesses an external service via a service endpoint we need to ensure
                    // that is also tracked as a required resource to execute this pipeline. 
                    var groupReference = ToGroupReference(variableGroupData.Key, variableGroupData.ToList());
                    if (groupReference?.SecretStore != null)
                    {
                        // Add the variable group reference to the list of required resources
                        context.ReferencedResources.VariableGroups.Add(groupReference);

                        // Add this resource as authorized for use by this pipeline since the variable group requires
                        // it to function and the variable group was successfully authorized.
                        if (groupReference.SecretStore.Endpoint != null)
                        {
                            this.ResourceStore.Endpoints.Authorize(groupReference.SecretStore.Endpoint);
                            context.ReferencedResources.Endpoints.Add(groupReference.SecretStore.Endpoint);
                        }

                        if (groupReference.SecretStore.Keys.Count == 0)
                        {
                            continue;
                        }

                        // Make sure we don't unnecessarily retrieve values
                        var valueProvider = this.ResourceStore.VariableGroups.GetValueProvider(groupReference);
                        if (valueProvider == null)
                        {
                            continue;
                        }

                        var variableGroup = this.ResourceStore.GetVariableGroup(groupReference);
                        ServiceEndpoint endpoint = null;
                        if (groupReference.SecretStore.Endpoint != null)
                        {
                            endpoint = this.ResourceStore.GetEndpoint(groupReference.SecretStore.Endpoint);
                            if (endpoint == null)
                            {
                                throw new DistributedTaskException(PipelineStrings.ServiceConnectionUsedInVariableGroupNotValid(groupReference.SecretStore.Endpoint, groupReference.Name));
                            }
                        }

                        if (!valueProvider.ShouldGetValues(context))
                        {
                            // This will ensure that no value is provided by the server since we expect it to be set by a task
                            foreach (var key in groupReference.SecretStore.Keys)
                            {
                                context.Trace?.Info($"{key}: $[ variablegroups.{variableGroup.Name}.{key} ]");
                                expressionsToEvaluate.Remove(key);
                                context.Variables[key] = new VariableValue(null, true);
                            }
                        }
                        else
                        {
                            var values = valueProvider.GetValues(variableGroup, endpoint, groupReference.SecretStore.Keys, includeSecrets);
                            if (values != null)
                            {
                                foreach (var value in values)
                                {
                                    context.Trace?.Info($"{value.Key}: $[ variablegroups.{variableGroup.Name}.{value.Key} ]");
                                    expressionsToEvaluate.Remove(value.Key);

                                    if (includeSecrets || !value.Value.IsSecret)
                                    {
                                        context.Variables[value.Key] = value.Value;
                                    }
                                    else
                                    {
                                        context.Variables[value.Key].Value = null;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Now resolve the expressions discovered earlier 
            if (expressionsToEvaluate.Count > 0)
            {
                foreach (var variableExpression in expressionsToEvaluate)
                {
                    context.Trace?.EnterProperty(variableExpression.Key);
                    var result = variableExpression.Value.GetValue(context);
                    context.Trace?.LeaveProperty(variableExpression.Key);

                    if (context.Variables.TryGetValue(variableExpression.Key, out VariableValue existingValue))
                    {
                        existingValue.Value = result.Value;
                        existingValue.IsSecret |= result.ContainsSecrets;
                    }
                    else
                    {
                        context.Variables[variableExpression.Key] = new VariableValue(result.Value, result.ContainsSecrets);
                    }
                }
            }

            // Filter out secret variables if we are not supposed to include them in the context
            if (!includeSecrets)
            {
                foreach (var secretValue in context.Variables.Values.Where(x => x.IsSecret))
                {
                    secretValue.Value = null;
                }
            }

            if (referencedVariableGroups.Count > 0 || expressionsToEvaluate.Count > 0)
            {
                context.Trace?.LeaveProperty("Variables");
            }
        }

        private static VariableGroupReference ToGroupReference(
            VariableGroup group,
            IList<String> keys)
        {
            if (group == null || keys == null || keys.Count == 0)
            {
                return null;
            }

            var storeConfiguration = ToSecretStoreConfiguration(group, keys);
            return new VariableGroupReference
            {
                Id = group.Id,
                Name = group.Name,
                GroupType = group.Type,
                SecretStore = storeConfiguration,
            };
        }

        private static SecretStoreConfiguration ToSecretStoreConfiguration(
            VariableGroup group,
            IList<String> keys)
        {
            if (keys.Count == 0)
            {
                return null;
            }

            var keyVaultData = group.ProviderData as AzureKeyVaultVariableGroupProviderData;
            var configuration = new SecretStoreConfiguration
            {
                StoreName = keyVaultData?.Vault ?? group.Name,
            };

            if (keyVaultData != null && keyVaultData.ServiceEndpointId != Guid.Empty)
            {
                configuration.Endpoint = new ServiceEndpointReference
                {
                    Id = keyVaultData.ServiceEndpointId,
                };
            }

            configuration.Keys.AddRange(keys);
            return configuration;
        }

        private sealed class VariableGroupComparer : IEqualityComparer<VariableGroup>
        {
            public Boolean Equals(
                VariableGroup x,
                VariableGroup y)
            {
                return x?.Id == y?.Id;
            }

            public Int32 GetHashCode(VariableGroup obj)
            {
                return obj.Id.GetHashCode();
            }
        }

        private IList<IVariable> m_userVariables;
        private VariablesDictionary m_systemVariables;
        private readonly IPipelineContext m_context;
        private static readonly Lazy<VariableGroupComparer> s_comparer = new Lazy<VariableGroupComparer>(() => new VariableGroupComparer());
    }
}
