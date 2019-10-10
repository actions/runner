using System;
using System.Collections.Generic;
using System.ComponentModel;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Pipelines.ContextData;

namespace GitHub.DistributedTask.Pipelines.Runtime
{
    /// <summary>
    /// Provides context necessary for the execution of a pipeline.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PhaseExecutionContext : GraphExecutionContext<PhaseInstance>
    {
        public PhaseExecutionContext(
            StageInstance stage = null,
            PhaseInstance phase = null,
            DictionaryContextData data = null,
            EvaluationOptions expressionOptions = null,
            ExecutionOptions executionOptions = null)
            : this(stage, phase, PipelineState.InProgress, data,
                  new CounterStore(), new PackageStore(), new ResourceStore(), new TaskStore(),
                  null, null, null, expressionOptions, executionOptions)
        {
        }

        /// <summary>
        /// Initializes a new <c>PipelineExecutionContext</c> instance using the specified task store.
        /// </summary>
        /// <param name="taskStore">The store which should be utilized for task reference resolution</param>
        /// <param name="resources">The additional pre-defined resources which should be utilized for resource resolution, like: Container</param>
        public PhaseExecutionContext(
            StageInstance stage,
            PhaseInstance phase,
            PipelineState state,
            DictionaryContextData data,
            ICounterStore counterStore,
            IPackageStore packageStore,
            IResourceStore resourceStore,
            ITaskStore taskStore,
            IList<IStepProvider> stepProviders,
            IPipelineIdGenerator idGenerator,
            IPipelineTraceWriter trace,
            EvaluationOptions expressionOptions,
            ExecutionOptions executionOptions)
            : base(phase, state, data, counterStore, packageStore, resourceStore, taskStore, stepProviders, idGenerator, trace, expressionOptions, executionOptions)
        {
            this.Stage = stage;
            if (this.Stage != null)
            {
                this.Stage.Identifier = this.IdGenerator.GetStageIdentifier(this.Stage.Name);
            }

            // Set the full identifier according to the current context
            this.Phase.Identifier = this.IdGenerator.GetPhaseIdentifier(this.Stage?.Name, this.Phase.Name);
        }

        /// <summary>
        /// The current stage which is being executed.
        /// </summary>
        public StageInstance Stage
        {
            get;
        }

        /// <summary>
        /// The current phase which is being executed.
        /// </summary>
        public PhaseInstance Phase
        {
            get
            {
                return base.Node;
            }
        }

        /// <summary>
        /// Gets the previous attempt of the phase if this is a retry of a job which has already executed.
        /// </summary>
        public PhaseAttempt PreviousAttempt
        {
            get;
            set;
        }

        public JobExecutionContext CreateJobContext(
            String name,
            Int32 attempt,
            Int32 positionInPhase = default,
            Int32 totalJobsInPhase = default)
        {
            return CreateJobContext(
                new JobInstance(name, attempt),
                positionInPhase,
                totalJobsInPhase);
        }

        public JobExecutionContext CreateJobContext(
            JobInstance jobInstance,
            Int32 positionInPhase = default,
            Int32 totalJobsInPhase = default)
        {
            return new JobExecutionContext(
                context: this,
                job: jobInstance,
                variables: null,
                positionInPhase: positionInPhase,
                totalJobsInPhase: totalJobsInPhase);
        }

        internal override String GetInstanceName()
        {
            return this.IdGenerator.GetPhaseInstanceName(this.Stage?.Name, this.Phase.Name, this.Phase.Attempt);
        }
    }
}
