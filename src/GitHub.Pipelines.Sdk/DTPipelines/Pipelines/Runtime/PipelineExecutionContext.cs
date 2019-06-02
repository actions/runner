using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.TeamFoundation.DistributedTask.Expressions;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Runtime
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class PipelineExecutionContext : PipelineContextBase
    {
        private protected PipelineExecutionContext(PipelineExecutionContext context)
            : base(context)
        {
            this.State = context.State;
            this.ExecutionOptions = context.ExecutionOptions;
        }

        private protected PipelineExecutionContext(
            ICounterStore counterStore,
            IPackageStore packageStore,
            IResourceStore resourceStore,
            ITaskStore taskStore,
            IList<IStepProvider> stepProviders,
            PipelineState state,
            IPipelineIdGenerator idGenerator = null,
            IPipelineTraceWriter trace = null,
            EvaluationOptions expressionOptions = null,
            ExecutionOptions executionOptions = null)
            : base(counterStore, packageStore, resourceStore, taskStore, stepProviders, idGenerator, trace, expressionOptions)
        {
            this.State = state;
            this.ExecutionOptions = executionOptions ?? new ExecutionOptions();
        }

        /// <summary>
        /// Gets the current state of the pipeline.
        /// </summary>
        public PipelineState State
        {
            get;
        }

        /// <summary>
        /// Gets the execution options used for the pipeline.
        /// </summary>
        public ExecutionOptions ExecutionOptions
        {
            get;
        }

        /// <summary>
        /// Gets the instance ID for the current context.
        /// </summary>
        /// <returns></returns>
        internal Guid GetInstanceId()
        {
            return this.IdGenerator.GetInstanceId(this.GetInstanceName());
        }

        /// <summary>
        /// When overridden in a derived class, gets the instance name using <see cref="PipelineContextBase.IdGenerator"/>.
        /// </summary>
        /// <returns>The instance name according to the associated generator</returns>
        internal abstract String GetInstanceName();
    }
}
