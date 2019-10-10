using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Logging;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Common;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides base functionality for all contexts used during build and execution if a pipeline.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class PipelineContextBase : IPipelineContext
    {
        private protected PipelineContextBase(IPipelineContext context)
        {
            this.EnvironmentVersion = context.EnvironmentVersion;
            this.SystemVariableNames.UnionWith(context.SystemVariableNames);
            this.Variables.AddRange(context.Variables);
            m_referencedResources = context.ReferencedResources?.Clone();

            this.CounterStore = context.CounterStore;
            this.IdGenerator = context.IdGenerator ?? new PipelineIdGenerator();
            this.PackageStore = context.PackageStore;
            this.ResourceStore = context.ResourceStore;
            this.TaskStore = context.TaskStore;
            this.Trace = context.Trace;
            m_secretMasker = new Lazy<ISecretMasker>(() => CreateSecretMasker());

            // This is a copy, don't dynamically set pipeline decorators, they are already set.
            this.StepProviders = context.StepProviders;

            if (context.Data?.Count > 0)
            {
                m_data = new DictionaryContextData();
                foreach (var pair in context.Data)
                {
                    m_data[pair.Key] = pair.Value;
                }
            }
        }

        private protected PipelineContextBase(
            DictionaryContextData data,
            ICounterStore counterStore,
            IPackageStore packageStore,
            IResourceStore resourceStore,
            ITaskStore taskStore,
            IList<IStepProvider> stepProviders,
            IPipelineIdGenerator idGenerator = null,
            IPipelineTraceWriter trace = null,
            EvaluationOptions expressionOptions = null)
        {
            m_data = data;
            this.CounterStore = counterStore;
            this.ExpressionOptions = expressionOptions ?? new EvaluationOptions();
            this.IdGenerator = idGenerator ?? new PipelineIdGenerator();
            this.PackageStore = packageStore;
            this.ResourceStore = resourceStore;
            this.TaskStore = taskStore;
            this.Trace = trace;
            m_secretMasker = new Lazy<ISecretMasker>(() => CreateSecretMasker());

            // Setup pipeline decorators
            var aggregatedStepProviders = new List<IStepProvider>();

            // Add resources first
            if (this.ResourceStore != null)
            {
                aggregatedStepProviders.Add(this.ResourceStore);
            }

            // Add custom pipeline decorators
            if (stepProviders != null)
            {
                aggregatedStepProviders.AddRange(stepProviders);
            }

            this.StepProviders = aggregatedStepProviders;
        }

        /// <summary>
        /// Gets the counter store for the current context
        /// </summary>
        public ICounterStore CounterStore
        {
            get;
        }

        // Gets the available context when evaluating expressions
        public DictionaryContextData Data
        {
            get
            {
                if (m_data == null)
                {
                    m_data = new DictionaryContextData();
                }
                return m_data;
            }
        }

        /// <summary>
        /// Gets or sets the version of the environment
        /// </summary>
        public Int32 EnvironmentVersion
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the expression evaluation options for this context.
        /// </summary>
        public EvaluationOptions ExpressionOptions
        {
            get;
        }

        /// <summary>
        /// Gets the id generator configured for this context.
        /// </summary>
        public IPipelineIdGenerator IdGenerator
        {
            get;
        }

        /// <summary>
        /// Gets the package store configured for this context.
        /// </summary>
        public IPackageStore PackageStore
        {
            get;
        }

        /// <summary>
        /// Gets the resources referenced within this context.
        /// </summary>
        public PipelineResources ReferencedResources
        {
            get
            {
                if (m_referencedResources == null)
                {
                    m_referencedResources = new PipelineResources();
                }
                return m_referencedResources;
            }
        }

        /// <summary>
        /// Gets the resource store for the current context
        /// </summary>
        public IResourceStore ResourceStore
        {
            get;
        }

        /// <summary>
        /// Gets the step providers for the current context
        /// </summary>
        public IReadOnlyList<IStepProvider> StepProviders
        {
            get;
        }

        /// <summary>
        /// Gets the secret masker for the current context
        /// </summary>
        public ISecretMasker SecretMasker
        {
            get
            {
                return m_secretMasker.Value;
            }
        }

        /// <summary>
        /// Gets the task store for the current context
        /// </summary>
        public ITaskStore TaskStore
        {
            get;
        }

        /// <summary>
        /// Gets the trace for the current context
        /// </summary>
        public IPipelineTraceWriter Trace
        {
            get;
        }

        /// <summary>
        /// Gets the system variable names for the current context
        /// </summary>
        public ISet<String> SystemVariableNames
        {
            get
            {
                if (m_systemVariables == null)
                {
                    m_systemVariables = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
                }
                return m_systemVariables;
            }
        }

        /// <summary>
        /// Gets the variables configured on the context
        /// </summary>
        public IDictionary<String, VariableValue> Variables
        {
            get
            {
                if (m_variables == null)
                {
                    m_variables = new VariablesDictionary();
                }
                return m_variables;
            }
        }

        /// <summary>
        /// Gets a value indicating if secret variables have been accessed
        /// </summary>
        protected virtual Boolean SecretsAccessed
        {
            get
            {
                return m_variables?.SecretsAccessed.Count > 0;
            }
        }

        public virtual ISecretMasker CreateSecretMasker()
        {
            var secretMasker = new SecretMasker();

            // Add variable secrets
            if (m_variables?.Count > 0)
            {
                foreach (var variable in m_variables.Values.Where(x => x.IsSecret))
                {
                    secretMasker.AddValue(variable.Value);
                }
            }

            return secretMasker;
        }

        /// <summary>
        /// Expand macros of the format $(variableName) using the current context.
        /// </summary>
        /// <param name="value">The value which contains macros to expand</param>
        /// <param name="maskSecrets">True if secrets should be replaced with '***'; otherwise, false</param>
        /// <returns>The evaluated value with all defined macros expanded to the value in the current context</returns>
        public String ExpandVariables(
            String value, 
            Boolean maskSecrets = false)
        {
            if (!String.IsNullOrEmpty(value) && m_variables?.Count > 0)
            {
                return VariableUtility.ExpandVariables(value, m_variables, false, maskSecrets);
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Expand macros of the format $(variableName) using the current context.
        /// </summary>
        /// <param name="value">The JToken value which contains macros to expand</param>
        /// <returns>The evaluated value with all defined macros expanded to the value in the current context</returns>
        public JToken ExpandVariables(JToken value)
        {
            if (value != null && m_variables?.Count > 0)
            {
                return VariableUtility.ExpandVariables(value, m_variables, false);
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Expand all variables and macros present as values (not keys) in a given JObject.
        /// Conditionally record unresolved expressions or macros as errors.
        /// </summary>
        public ExpressionResult<JObject> Evaluate(JObject value)
        {
            if (value == null)
            {
                return null;
            }

            var containsSecrets = false;
            String ResolveExpression(String s)
            {
                if (!ExpressionValue.IsExpression(s))
                {
                    return s;
                }

                String resolvedValue = null;
                try
                {
                    var expressionResult = Evaluate<String>(ExpressionValue.TrimExpression(s));
                    containsSecrets |= expressionResult.ContainsSecrets;
                    resolvedValue = expressionResult.Value;
                }
                catch (ExpressionException)
                {
                    return s;
                }

                if (!String.IsNullOrEmpty(resolvedValue))
                {
                    return resolvedValue;
                }

                return s;
            }

            // recurse through object
            var resolvedSpec = new JObject();
            foreach (var pair in value)
            {
                var v = pair.Value;
                switch (v.Type)
                {
                    case JTokenType.Object:
                        // recurse
                        var expressionResult = Evaluate(v.Value<JObject>());
                        containsSecrets |= expressionResult.ContainsSecrets;
                        resolvedSpec[pair.Key] = expressionResult.Value;
                        break;
                    case JTokenType.String:
                        // resolve
                        resolvedSpec[pair.Key] = ExpandVariables(ResolveExpression(v.Value<String>()));
                        break;
                    default:
                        // no special handling
                        resolvedSpec[pair.Key] = v;
                        break;
                }
            }

            return new ExpressionResult<JObject>(resolvedSpec, containsSecrets);
        }

        /// <summary>
        /// Evalutes the provided expression using the current context.
        /// </summary>
        /// <typeparam name="T">The type of result expected</typeparam>
        /// <param name="expression">The expression string to evaluate</param>
        /// <returns>A value indicating the evaluated result and whether or not secrets were accessed during evaluation</returns>
        public ExpressionResult<T> Evaluate<T>(String expression)
        {
            if (!m_parsedExpressions.TryGetValue(expression, out IExpressionNode parsedExpression))
            {
                parsedExpression = new ExpressionParser().CreateTree(expression, this.Trace, this.GetSupportedNamedValues(), this.GetSupportedFunctions());
                m_parsedExpressions.Add(expression, parsedExpression);
            }

            this.ResetSecretsAccessed();
            var evaluationResult = parsedExpression.Evaluate(this.Trace, this.SecretMasker, this, this.ExpressionOptions);
            throw new NotSupportedException();
        }

        protected virtual void ResetSecretsAccessed()
        {
            m_variables?.SecretsAccessed.Clear();
        }

        protected virtual IEnumerable<IFunctionInfo> GetSupportedFunctions()
        {
            return Enumerable.Empty<IFunctionInfo>();
        }

        protected virtual IEnumerable<INamedValueInfo> GetSupportedNamedValues()
        {
            return Enumerable.Empty<INamedValueInfo>();
        }

        internal void SetSystemVariables(IEnumerable<Variable> variables)
        {
            foreach (var variable in variables)
            {
                this.SystemVariableNames.Add(variable.Name);
                this.Variables[variable.Name] = new VariableValue(variable.Value, variable.Secret);
            }
        }

        internal void SetUserVariables(IEnumerable<Variable> variables)
        {
            foreach (var variable in variables.Where(x=>!x.Name.StartsWith("system.", StringComparison.OrdinalIgnoreCase) && !this.SystemVariableNames.Contains(x.Name)))
            {
                this.Variables[variable.Name] = new VariableValue(variable.Value, variable.Secret);
            }
        }

        internal void SetUserVariables(IDictionary<String, String> variables)
        {
            // Do not allow user variables to override system variables which were set at a higher scope. In this case
            // the system variable should always win rather than the most specific variable winning.
            foreach (var variable in variables.Where(x => !x.Key.StartsWith("system.", StringComparison.OrdinalIgnoreCase) && !this.SystemVariableNames.Contains(x.Key)))
            {
                this.Variables[variable.Key] = variable.Value;
            }
        }

        internal void SetSystemVariables(IDictionary<String, VariableValue> variables)
        {
            if (variables?.Count > 0)
            {
                foreach (var variable in variables)
                {
                    this.SystemVariableNames.Add(variable.Key);
                    this.Variables[variable.Key] = variable.Value?.Clone();
                }
            }
        }

        private DictionaryContextData m_data;
        private VariablesDictionary m_variables;
        private HashSet<String> m_systemVariables;
        private Lazy<ISecretMasker> m_secretMasker;
        private PipelineResources m_referencedResources;
        private Dictionary<String, IExpressionNode> m_parsedExpressions = new Dictionary<String, IExpressionNode>(StringComparer.OrdinalIgnoreCase);
    }
}
