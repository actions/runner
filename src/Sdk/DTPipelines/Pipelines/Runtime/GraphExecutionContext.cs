using System.Collections.Generic;
using System.ComponentModel;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Pipelines.ContextData;
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
        }

        private protected GraphExecutionContext(
            TInstance node,
            PipelineState state,
            DictionaryContextData data,
            ICounterStore counterStore,
            IPackageStore packageStore,
            IResourceStore resourceStore,
            ITaskStore taskStore,
            IList<IStepProvider> stepProviders,
            IPipelineIdGenerator idGenerator = null,
            IPipelineTraceWriter trace = null,
            EvaluationOptions expressionOptions = null,
            ExecutionOptions executionOptions = null)
            : base(data, counterStore, packageStore, resourceStore, taskStore, stepProviders, state, idGenerator, trace, expressionOptions, executionOptions)
        {
            ArgumentUtility.CheckForNull(node, nameof(node));

            this.Node = node;
        }

        /// <summary>
        /// Gets the target node for this execution context.
        /// </summary>
        protected TInstance Node
        {
            get;
        }
    }
}
