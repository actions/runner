﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.Pipelines.Runtime;
using GitHub.DistributedTask.Pipelines.Validation;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class JobFactory : PhaseNode
    {
        public JobFactory()
        {
        }

        private JobFactory(JobFactory copy)
            : base(copy)
        {
            if (copy.m_steps != null && copy.m_steps.Count > 0)
            {
                m_steps = new List<Step>(copy.m_steps.Select(x => x.Clone()));
            }
        }

        /// <summary>
        /// Gets the phase type.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public override PhaseType Type => PhaseType.JobFactory;

        public IList<ContextScope> Scopes
        {
            get
            {
                if (m_scopes == null)
                {
                    m_scopes = new List<ContextScope>();
                }
                return m_scopes;
            }
        }

        /// <summary>
        /// Gets the list of steps associated with this phase. At runtime the steps will be used as a template for
        /// the execution of a job.
        /// </summary>
        public IList<Step> Steps
        {
            get
            {
                if (m_steps == null)
                {
                    m_steps = new List<Step>();
                }
                return m_steps;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken Strategy
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public ScalarToken JobDisplayName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken JobTarget
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public ScalarToken JobTimeout
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public ScalarToken JobCancelTimeout
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken JobContainer
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken JobServiceContainers
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken EnvironmentVariables
        {
            get;
            set;
        }

        public void CheckExpandReferences(
            out bool isEventReferenced,
            out bool isOutputsReferenced)
        {
            isEventReferenced = false;
            isOutputsReferenced = false;
            var expressionTokens = Strategy.Traverse()
                .Concat(JobDisplayName.Traverse())
                .Concat(JobTarget.Traverse())
                .Concat(JobTimeout.Traverse())
                .Concat(JobCancelTimeout.Traverse())
                .OfType<BasicExpressionToken>()
                .ToArray();
            var parser = new ExpressionParser();
            foreach (var expressionToken in expressionTokens)
            {
                var tree = parser.ValidateSyntax(expressionToken.Expression, null);
                var isReferenced = tree.CheckReferencesContext(
                    PipelineTemplateConstants.EventPattern,
                    PipelineTemplateConstants.OutputsPattern);
                if (!isEventReferenced)
                {
                    isEventReferenced = isReferenced[0];
                }

                if (!isOutputsReferenced)
                {
                    isOutputsReferenced = isReferenced[1];
                }
            }
        }

        public ExpandPhaseResult Expand(
            PhaseExecutionContext context,
            JobExpansionOptions options = null)
        {
            var result = new ExpandPhaseResult();

            var trace = new JobFactoryTrace(context.Trace);
            var schema = new PipelineTemplateSchemaFactory().CreateSchema();
            var templateEvaluator = new PipelineTemplateEvaluator(trace, schema);

            trace.Info("Evaluating strategy");
            var displayName = JobDisplayName is ExpressionToken ? null : DisplayName;
            var strategy = templateEvaluator.EvaluateStrategy(Strategy, context.Data, displayName);

            foreach (var jobContext in ExpandContexts(context, options, strategy, trace, templateEvaluator))
            {
                result.Jobs.Add(jobContext.Job);
            }

            if (strategy.MaxParallel > 0)
            {
                result.MaxConcurrency = strategy.MaxParallel;
            }
            else
            {
                result.MaxConcurrency = result.Jobs.Count;
            }

            result.FailFast = strategy.FailFast;
            
            return result;
        }

        public IEnumerable<JobExecutionContext> ExpandContexts(
            PhaseExecutionContext context,
            JobExpansionOptions options = null,
            StrategyResult strategy = null,
            DistributedTask.ObjectTemplating.ITraceWriter trace = null,
            PipelineTemplateEvaluator templateEvaluator = null)
        {
            if (trace == null)
            {
                trace = new JobFactoryTrace(context.Trace);
            }

            if (templateEvaluator == null)
            {
                var schema = new PipelineTemplateSchemaFactory().CreateSchema();
                templateEvaluator = new PipelineTemplateEvaluator(trace, schema);
            }

            // Strategy
            if (strategy == null)
            {
                trace.Info("Evaluating strategy");
                var displayName = JobDisplayName is ExpressionToken ? null : DisplayName;
                strategy = templateEvaluator.EvaluateStrategy(Strategy, context.Data, displayName);
            }

            // Check max jobs
            var maxJobs = context.ExecutionOptions.MaxJobExpansion ?? 100;
            if (strategy.Configurations.Count > maxJobs)
            {
                throw new MaxJobExpansionException($"Strategy produced more than {maxJobs}");
            }

            // Create jobs
            for (var i = 0; i < strategy.Configurations.Count; i++)
            {
                var configuration = strategy.Configurations[i];
                var jobName = configuration.Name;
                var attempt = 1;
                if (options?.Configurations.Count > 0)
                {
                    if (!options.Configurations.TryGetValue(jobName, out attempt))
                    {
                        continue;
                    }
                }

                yield return CreateJob(trace, context, templateEvaluator, jobName, configuration.DisplayName, attempt, i + 1, strategy.Configurations.Count, configuration.ContextData);
            }
        }

        /// <summary>
        /// Resolves external references and ensures the steps are compatible with the selected target.
        /// </summary>
        /// <param name="context">The validation context</param>
        public override void Validate(
            PipelineBuildContext context,
            ValidationResult result)
        {
            base.Validate(context, result);

            var phaseStepValidationResult = new Phase.StepValidationResult();

            // Require the latest agent version.
            if (context.BuildOptions.DemandLatestAgent)
            {
                var latestPackageVersion = context.PackageStore?.GetLatestVersion(WellKnownPackageTypes.Agent);
                if (latestPackageVersion == null)
                {
                    throw new NotSupportedException("Unable to determine the latest agent package version");
                }

                phaseStepValidationResult.MinAgentVersion = latestPackageVersion.ToString();
            }

            Phase.ValidateSteps(context, this, new AgentQueueTarget(), result, Steps, phaseStepValidationResult);

            // Resolve the target to ensure we have stable identifiers for the orchestration engine
            // phase targets with expressions need to be evaluated against resolved job contexts.
            bool validateTarget = false;
            if (this.Target.Type == PhaseTargetType.Pool || this.Target.Type == PhaseTargetType.Server)
            {
                validateTarget = true;
            }
            else if (this.Target is AgentQueueTarget agentQueueTarget && agentQueueTarget.IsLiteral())
            {
                validateTarget = true;
            }

            if (validateTarget)
            {
                this.Target.Validate(
                    context,
                    context.BuildOptions,
                    result,
                    this.Steps,
                    phaseStepValidationResult.TaskDemands);
            }
        }

        private JobExecutionContext CreateJob(
            DistributedTask.ObjectTemplating.ITraceWriter trace,
            PhaseExecutionContext phaseContext,
            PipelineTemplateEvaluator templateEvaluator,
            String jobName,
            String configurationDisplayName,
            Int32 attempt,
            Int32 positionInPhase,
            Int32 totalJobsInPhase,
            IDictionary<String, PipelineContextData> contextData)
        {
            trace.Info($"Creating job '{jobName}'");
            var jobContext = new JobExecutionContext(
                context: phaseContext,
                job: new JobInstance(jobName, attempt),
                variables: null,
                positionInPhase: positionInPhase,
                totalJobsInPhase: totalJobsInPhase,
                data: contextData);
            var job = new Job
            {
                Id = jobContext.GetInstanceId(),
                Name = jobContext.Job.Name,
                EnvironmentVariables = EnvironmentVariables,
                Container = JobContainer,
                ServiceContainers = JobServiceContainers,
            };

            if (JobDisplayName is ExpressionToken)
            {
                trace.Info("Evaluating display name");
                job.DisplayName = templateEvaluator.EvaluateJobDisplayName(JobDisplayName, jobContext.Data, DisplayName);
            }
            else if (!String.IsNullOrEmpty(configurationDisplayName))
            {
                job.DisplayName = configurationDisplayName;
            }
            else
            {
                job.DisplayName = DisplayName;
            }

            trace.Info("Evaluating timeout");
            job.TimeoutInMinutes = templateEvaluator.EvaluateJobTimeout(JobTimeout, jobContext.Data);
            trace.Info("Evaluating cancel timeout");
            job.CancelTimeoutInMinutes = templateEvaluator.EvaluateJobCancelTimeout(JobCancelTimeout, jobContext.Data);
            trace.Info("Evaluating target");
            job.Target = templateEvaluator.EvaluateJobTarget(JobTarget, jobContext.Data);
            
            jobContext.Job.Definition = job;

            // Resolve the pool by name
            if (job.Target is AgentPoolTarget pool &&
                pool.Pool?.Id == 0 &&
                !String.IsNullOrEmpty(pool.Pool.Name?.Literal))
            {
                var resolved = jobContext.ResourceStore.GetPool(pool.Pool.Name.Literal);
                if (resolved != null)
                {
                    pool.Pool = new AgentPoolReference { Id = resolved.Id, Name = resolved.Name };
                }
            }

            // Resolve the queue by name
            if (job.Target is AgentQueueTarget queue &&
                queue.Queue?.Id == 0 &&
                !String.IsNullOrEmpty(queue.Queue.Name?.Literal))
            {
                var resolved = jobContext.ResourceStore.GetQueue(queue.Queue.Name.Literal);
                if (resolved != null)
                {
                    queue.Queue = new AgentQueueReference { Id = resolved.Id, Name = resolved.Name };
                }
            }

            // Always add self
            var self = jobContext.ResourceStore?.Repositories.Get(PipelineConstants.SelfAlias);
            if (self == null)
            {
                throw new InvalidOperationException($"Repository '{PipelineConstants.SelfAlias}' not found");
            }

            jobContext.ReferencedResources.Repositories.Add(self);

            // Add the endpoint
            if (self.Endpoint != null)
            {
                jobContext.ReferencedResources.AddEndpointReference(self.Endpoint);
                var repositoryEndpoint = jobContext.ResourceStore?.GetEndpoint(self.Endpoint);
                if (repositoryEndpoint == null)
                {
                    throw new ResourceNotFoundException(PipelineStrings.ServiceEndpointNotFound(self.Endpoint));
                }
            }

            // Update the execution context with the job-specific system variables
            UpdateJobContextVariablesFromJob(jobContext, job);

            var steps = new List<JobStep>();
            var identifier = jobContext.GetInstanceName();
            foreach (var step in Steps)
            {
                if (step.Type == StepType.Action)
                {
                    job.Steps.Add(Phase.CreateJobActionStep(jobContext, identifier, step as ActionStep));
                }
                else
                {
                    throw new NotSupportedException($"Unexpected step type '{step.Type}'");
                }
            }

            foreach (var scope in Scopes)
            {
                job.Scopes.Add(scope);
            }

            return jobContext;
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_scopes?.Count == 0)
            {
                m_scopes = null;
            }

            if (m_steps?.Count == 0)
            {
                m_steps = null;
            }
        }

        private sealed class JobFactoryTrace : DistributedTask.ObjectTemplating.ITraceWriter
        {
            public JobFactoryTrace(DistributedTask.Expressions2.ITraceWriter trace)
            {
                m_trace = trace;
            }

            public void Error(
                String message,
                params Object[] args)
            {
                Info("##[error]", message, args);
            }

            public void Info(
                String message,
                params Object[] args)
            {
                Info(String.Empty, message, args);
            }

            public void Verbose(
                String message,
                params Object[] args)
            {
                Info("##[debug]", message, args);
            }

            private void Info(
                String prefix,
                String message,
                params Object[] args)
            {
                if (m_trace == null)
                {
                    return;
                }

                if (args?.Length > 0)
                {
                    m_trace.Info(String.Format(CultureInfo.InvariantCulture, $"{prefix}{message}", args));
                }
                else
                {
                    m_trace.Info($"{prefix}{message}");
                }
            }

            private DistributedTask.Expressions2.ITraceWriter m_trace;
        }

        [DataMember(Name = "Scopes", EmitDefaultValue = false)]
        private IList<ContextScope> m_scopes;

        [DataMember(Name = "Steps", EmitDefaultValue = false)]
        private IList<Step> m_steps;
    }
}
