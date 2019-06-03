using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GitHub.DistributedTask.Expressions;
using GitHub.DistributedTask.Logging;
using GitHub.DistributedTask.Pipelines.Expressions;
using GitHub.Services.Common;

namespace GitHub.DistributedTask.Pipelines.Runtime
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class GraphExecutionContext<TInstance> : PipelineExecutionContext where TInstance : IGraphNodeInstance
    {
        private protected GraphExecutionContext(GraphExecutionContext<TInstance> context)
            : base(context)
        {
            this.Node = context.Node;

            if (context.m_dependencies?.Count > 0)
            {
                m_dependencies = context.m_dependencies.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
            }
        }

        private protected GraphExecutionContext(
            TInstance node,
            IDictionary<String, TInstance> dependencies,
            PipelineState state,
            ICounterStore counterStore,
            IPackageStore packageStore,
            IResourceStore resourceStore,
            ITaskStore taskStore,
            IList<IStepProvider> stepProviders,
            IPipelineIdGenerator idGenerator = null,
            IPipelineTraceWriter trace = null,
            EvaluationOptions expressionOptions = null,
            ExecutionOptions executionOptions = null)
            : base(counterStore, packageStore, resourceStore, taskStore, stepProviders, state, idGenerator, trace, expressionOptions, executionOptions)
        {
            ArgumentUtility.CheckForNull(node, nameof(node));

            this.Node = node;

            if (dependencies?.Count > 0)
            {
                m_dependencies = dependencies.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
            }
        }

        public IDictionary<String, TInstance> Dependencies
        {
            get
            {
                if (m_dependencies == null)
                {
                    m_dependencies = new Dictionary<String, TInstance>(StringComparer.OrdinalIgnoreCase);
                }
                return m_dependencies;
            }
        }

        /// <summary>
        /// Gets the target node for this execution context.
        /// </summary>
        protected TInstance Node
        {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether or not secret variables have been accessed.
        /// </summary>
        protected override Boolean SecretsAccessed
        {
            get
            {
                return base.SecretsAccessed || (m_dependencies?.Any(x => x.Value.SecretsAccessed) ?? false);
            }
        }


        protected override void ResetSecretsAccessed()
        {
            base.ResetSecretsAccessed();

            if (m_dependencies?.Count > 0)
            {
                foreach (var dependency in m_dependencies)
                {
                    dependency.Value.ResetSecretsAccessed();
                }
            }
        }

        public override ISecretMasker CreateSecretMasker()
        {
            var secretMasker = base.CreateSecretMasker();

            // Add output variable secrets
            if (m_dependencies?.Count > 0)
            {
                foreach (var phase in m_dependencies.Values)
                {
                    foreach (var variable in phase.Outputs.Values.Where(x => x.IsSecret))
                    {
                        secretMasker.AddValue(variable.Value);
                    }
                }
            }

            return secretMasker;
        }

        protected override IEnumerable<INamedValueInfo> GetSupportedNamedValues()
        {
            foreach (var namedValue in base.GetSupportedNamedValues())
            {
                yield return namedValue;
            }

            yield return new NamedValueInfo<DependenciesContextNode<TInstance>>("dependencies");
        }

        private Dictionary<String, TInstance> m_dependencies;
    }
}
