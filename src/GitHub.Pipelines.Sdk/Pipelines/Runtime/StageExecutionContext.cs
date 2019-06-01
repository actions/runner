using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.Expressions;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Runtime
{
    /// <summary>
    /// Provides context necessary for the execution of a pipeline.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class StageExecutionContext : GraphExecutionContext<StageInstance>
    {
        public StageExecutionContext(
            StageInstance stage = default,
            IList<StageInstance> dependencies = null)
            : this(stage, dependencies?.ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase))
        {
        }

        /// <summary>
        /// Initializes a new <c>PipelineExecutionContext</c> instance.
        /// </summary>
        public StageExecutionContext(
            StageInstance stage,
            IDictionary<String, StageInstance> dependencies)
            : this(stage, dependencies, PipelineState.InProgress, new CounterStore(), new PackageStore(), new ResourceStore(), new TaskStore(), null, null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new <c>StageExecutionContext</c> instance using the specified stage and services.
        /// </summary>
        /// <param name="taskStore">The store which should be utilized for task reference resolution</param>
        /// <param name="resources">The additional pre-defined resources which should be utilized for resource resolution, like: Container</param>
        public StageExecutionContext(
            StageInstance stage,
            IDictionary<String, StageInstance> dependencies,
            PipelineState state,
            ICounterStore counterStore,
            IPackageStore packageStore,
            IResourceStore resourceStore,
            ITaskStore taskStore,
            IList<IStepProvider> stepProviders,
            IPipelineIdGenerator idGenerator,
            IPipelineTraceWriter trace,
            EvaluationOptions expressionOptions,
            ExecutionOptions executionOptions)
            : base(stage, dependencies, state, counterStore, packageStore, resourceStore, taskStore, stepProviders, idGenerator, trace, expressionOptions, executionOptions)
        {
            this.Stage.Identifier = this.IdGenerator.GetStageIdentifier(stage.Name);
        }

        /// <summary>
        /// The current stage which is being executed.
        /// </summary>
        public StageInstance Stage => this.Node;

        /// <summary>
        /// Gets the previous attempt of the stage if this is a retry of a job which has already executed.
        /// </summary>
        public StageAttempt PreviousAttempt
        {
            get;
        }

        internal override String GetInstanceName()
        {
            return this.IdGenerator.GetStageInstanceName(this.Stage.Name, this.Stage.Attempt);
        }
    }
}
