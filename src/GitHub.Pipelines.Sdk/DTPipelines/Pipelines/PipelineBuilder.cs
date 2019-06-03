using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using GitHub.DistributedTask.Expressions;
using GitHub.DistributedTask.Pipelines.Runtime;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Common;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides a mechanism for customization of pipeline construction by different hosting environments.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PipelineBuilder
    {
        /// <summary>
        /// Initializes a new <c>PipelineBuilder</c> instance with default service implementations.
        /// </summary>
        public PipelineBuilder()
            : this(null, null, null, null, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Initilaizes a new <c>PipelineBuilder</c> instance with the specified services used for resolution of
        /// resources available to pipelines.
        /// </summary>
        /// <param name="resourceStore">The resources available for use within a pipeline</param>
        /// <param name="taskStore">The tasks available for use within a pipeline</param>
        /// <param name="secretStore">The secret stores available for use within a pipeline</param>
        public PipelineBuilder(
            EvaluationOptions expressionOptions = null,
            ICounterStore counterStore = null,
            IPackageStore packageStore = null,
            IResourceStore resourceStore = null,
            IList<IStepProvider> stepProviders = null,
            ITaskStore taskStore = null,
            ITaskTemplateStore templateStore = null,
            IPipelineIdGenerator idGenerator = null,
            IList<IPhaseProvider> phaseProviders = null)
            : this(new PipelineContextBuilder(counterStore, packageStore, resourceStore, stepProviders, taskStore, idGenerator, expressionOptions, phaseProviders), templateStore)
        {
        }

        /// <summary>
        /// Initializes a new <c>PipelineBuilder</c> instance for the specified context.
        /// </summary>
        /// <param name="context">The context which should be used for validation</param>
        internal PipelineBuilder(IPipelineContext context)
            : this(new PipelineContextBuilder(context))
        {
        }

        private PipelineBuilder(
            PipelineContextBuilder contextBuilder,
            ITaskTemplateStore templateStore = null)
        {
            ArgumentUtility.CheckForNull(contextBuilder, nameof(contextBuilder));

            m_contextBuilder = contextBuilder;
            m_templateStore = templateStore;
        }

        /// <summary>
        /// Gets or sets the default queue which is assigned automatically to phases with no target and existing agent 
        /// queue targets without a queue specified.
        /// </summary>
        public AgentQueueReference DefaultQueue
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default agent specification which is assigned automatically to phases with no target and existing agent 
        /// queue targets without an agent specification specified.
        /// </summary>
        public JObject DefaultAgentSpecification
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default checkout options which are propagated to all repository resources defined
        /// within a pipeline if explicit options are not provided at checkout.
        /// </summary>
        public CheckoutOptions DefaultCheckoutOptions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default workspace options which are propagated to all agent jobs which do not explicitly
        /// define overrides.
        /// </summary>
        public WorkspaceOptions DefaultWorkspaceOptions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the environment version which should be used.
        /// </summary>
        public Int32 EnvironmentVersion
        {
            get
            {
                return m_contextBuilder.EnvironmentVersion;
            }
            set
            {
                m_contextBuilder.EnvironmentVersion = value;
            }
        }

        /// <summary>
        /// Gets the counter store configured for this builder.
        /// </summary>
        public ICounterStore CounterStore => m_contextBuilder.CounterStore;

        /// <summary>
        /// Gets the ID generator configured for this builder.
        /// </summary>
        public IPipelineIdGenerator IdGenerator => m_contextBuilder.IdGenerator;

        /// <summary>
        /// Gets the package store configured for this builder.
        /// </summary>
        public IPackageStore PackageStore => m_contextBuilder.PackageStore;

        /// <summary>
        /// Gets the resource store configured for this builder.
        /// </summary>
        public IResourceStore ResourceStore => m_contextBuilder.ResourceStore;

        /// <summary>
        /// Gets the user variables which have been set for this builder.
        /// </summary>
        public IList<IVariable> UserVariables => m_contextBuilder.UserVariables;

        /// <summary>
        /// Gets the system variables which have been set for this builder.
        /// </summary>
        public IDictionary<String, VariableValue> SystemVariables => m_contextBuilder.SystemVariables;

        /// <summary>
        /// Builds a new <c>PipelineProcess</c> instance using the specified environment.
        /// </summary>
        /// <param name="phases">A list of phases which should be used to build the process</param>
        /// <param name="options">The validation which should be performed on the pipeline</param>
        /// <returns>A <c>PipelineBuildResult</c> which contains a runnable pipeline process</returns>
        public PipelineBuildResult Build(
            IList<PhaseNode> phases,
            BuildOptions options = null)
        {
            ArgumentUtility.CheckEnumerableForEmpty(phases, nameof(phases));

            options = options ?? BuildOptions.None;

            var stage = new Stage
            {
                Name = PipelineConstants.DefaultJobName,
            };

            stage.Phases.AddRange(phases);
            return Build(new[] { stage }, options);
        }

        /// <summary>
        /// Builds a new <c>PipelineProcess</c> instance.
        /// </summary>
        /// <param name="stages">A list of stages which should be used to build the process</param>
        /// <param name="options">The validation which should be performed on the pipeline</param>
        /// <returns>A <c>PipelineBuildResult</c> which contains a runnable pipeline process</returns>
        public PipelineBuildResult Build(
            IList<Stage> stages,
            BuildOptions options = null)
        {
            var context = CreateBuildContext(options);

            if (this.DefaultCheckoutOptions != null)
            {
                foreach (var repository in context.ResourceStore.Repositories.GetAll())
                {
                    if (!repository.Properties.TryGetValue<CheckoutOptions>(RepositoryPropertyNames.CheckoutOptions, out _))
                    {
                        repository.Properties.Set(RepositoryPropertyNames.CheckoutOptions, this.DefaultCheckoutOptions.Clone());
                    }
                }
            }

            // First gather any validation errors and all referenced resources
            var process = CreateProcess(context, stages);
            var result = context.Validate(process);
            
            // Output the environment
            var environment = result.Environment = new PipelineEnvironment();
            environment.Version = context.EnvironmentVersion;
            environment.Counters.AddRange(context.CounterStore.Counters);
            environment.Resources.MergeWith(context.ResourceStore.GetAuthorizedResources());
            environment.UserVariables.AddRange(m_contextBuilder.UserVariables);
            environment.SystemVariables.AddRange(m_contextBuilder.SystemVariables);

            return new PipelineBuildResult(result.Environment, process, result);
        }

        public PipelineBuildContext CreateBuildContext(
            BuildOptions options,
            Boolean includeSecrets = false)
        {
            return m_contextBuilder.CreateBuildContext(options, this.PackageStore, includeSecrets);
        }

        public PhaseExecutionContext CreatePhaseExecutionContext(
            StageInstance stage,
            PhaseInstance phase,
            IDictionary<String, PhaseInstance> dependencies = null,
            PipelineState state = PipelineState.InProgress,
            Boolean includeSecrets = false,
            IPipelineTraceWriter trace = null,
            ExecutionOptions executionOptions = null)
        {
            return m_contextBuilder.CreatePhaseExecutionContext(stage, phase, dependencies, state, includeSecrets, trace, executionOptions);
        }

        public StageExecutionContext CreateStageExecutionContext(
            StageInstance stage,
            IDictionary<String, StageInstance> dependencies = null,
            PipelineState state = PipelineState.InProgress,
            Boolean includeSecrets = false,
            IPipelineTraceWriter trace = null,
            ExecutionOptions executionOptions = null)
        {
            return m_contextBuilder.CreateStageExecutionContext(stage, dependencies, state, includeSecrets, trace, executionOptions);
        }

        public IList<PipelineValidationError> Validate(
            PipelineProcess process,
            BuildOptions options = null)
        {
            ArgumentUtility.CheckForNull(process, nameof(process));

            var context = CreateBuildContext(options);
            return context.Validate(process).Errors;
        }

        public IList<PipelineValidationError> Validate(
            IList<Step> steps,
            PhaseTarget target = null,
            BuildOptions options = null)
        {
            ArgumentUtility.CheckForNull(steps, nameof(steps));

            var phase = new Phase();
            phase.Steps.AddRange(steps);
            phase.Target = target;

            var stage = new Stage(PipelineConstants.DefaultJobName, new[] { phase });

            var context = CreateBuildContext(options);
            var process = CreateProcess(context, new[] { stage });
            return context.Validate(process).Errors;
        }

        public PipelineResources GetReferenceResources(
            IList<Step> steps,
            PhaseTarget target = null)
        {
            ArgumentUtility.CheckForNull(steps, nameof(steps));

            var phase = new Phase();
            phase.Steps.AddRange(steps);
            phase.Target = target;

            var stage = new Stage(PipelineConstants.DefaultJobName, new[] { phase });

            var context = CreateBuildContext(new BuildOptions());
            var process = CreateProcess(context, new[] { stage });
            return context.Validate(process).ReferencedResources;
        }

        private PipelineProcess CreateProcess(
            PipelineBuildContext context,
            IList<Stage> stages)
        {
            ArgumentUtility.CheckForNull(context, nameof(context));
            ArgumentUtility.CheckEnumerableForEmpty(stages, nameof(stages));

            // Now inject the tasks into each appropriate phase
            foreach (var stage in stages)
            {
                foreach (var phaseNode in stage.Phases)
                {
                    // Set the default target to be a queue target and leave it up to the hosting environment to
                    // specify a default in the resource store.
                    if (phaseNode.Target == null)
                    {
                        phaseNode.Target = new AgentQueueTarget();
                    }

                    // Agent queues are the default target type
                    if (phaseNode.Target.Type == PhaseTargetType.Queue && this.DefaultQueue != null)
                    {
                        var queueTarget = phaseNode.Target as AgentQueueTarget;
                        var useDefault = false;
                        var queue = queueTarget.Queue;
                        if (queue == null)
                        {
                            useDefault = true;
                        }
                        else if (queue.Id == 0)
                        {
                            var name = queue.Name;
                            if (name == null || (name.IsLiteral && String.IsNullOrEmpty(name.Literal)))
                            {
                                useDefault = true;
                            }
                        }

                        if (useDefault)
                        {
                            queueTarget.Queue = this.DefaultQueue.Clone();
                            if (queueTarget.AgentSpecification == null)
                            {
                                queueTarget.AgentSpecification = (JObject)DefaultAgentSpecification?.DeepClone();
                            }
                        }
                    }

                    // Set default workspace options
                    if (phaseNode.Target.Type == PhaseTargetType.Queue && this.DefaultWorkspaceOptions != null)
                    {
                        var queueTarget = phaseNode.Target as AgentQueueTarget;
                        if (queueTarget.Workspace == null)
                        {
                            queueTarget.Workspace = this.DefaultWorkspaceOptions.Clone();
                        }
                    }

                    var steps = default(IList<Step>);
                    if (phaseNode.Type == PhaseType.Phase)
                    {
                        steps = (phaseNode as Phase).Steps;
                    }
                    else if (phaseNode.Type == PhaseType.JobFactory)
                    {
                        steps = (phaseNode as JobFactory).Steps;
                    }

                    if (steps != null)
                    {
                        var resolvedSteps = new List<Step>();
                        foreach (var step in steps.Where(x => x.Enabled))
                        {
                            if (step.Type == StepType.Task)
                            {
                                var taskStep = step as TaskStep;
                                resolvedSteps.Add(taskStep);
                            }
                            else if (step.Type == StepType.Group)
                            {
                                var taskStepGroup = step as GroupStep;
                                resolvedSteps.Add(taskStepGroup);
                            }
                            else if (step.Type == StepType.Action)
                            {
                                var actionStep = step as ActionStep;
                                resolvedSteps.Add(actionStep);
                            }
                            else if (step.Type == StepType.TaskTemplate)
                            {
                                var templateStep = step as TaskTemplateStep;
                                if (m_templateStore == null)
                                {
                                    throw new ArgumentException(PipelineStrings.TemplateStoreNotProvided(templateStep.Name, nameof(ITaskTemplateStore)));
                                }

                                var resolvedTasks = m_templateStore.ResolveTasks(templateStep);
                                resolvedSteps.AddRange(resolvedTasks);
                            }
                            else
                            {
                                // We should never be here.
                                Debug.Fail(step.Type.ToString());
                            }
                        }

                        steps.Clear();
                        steps.AddRange(resolvedSteps);
                    }
                }
            }

            return new PipelineProcess(stages);
        }

        private readonly ITaskTemplateStore m_templateStore;
        private readonly PipelineContextBuilder m_contextBuilder;
    }
}
