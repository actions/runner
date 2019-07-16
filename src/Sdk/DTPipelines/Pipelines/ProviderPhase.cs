using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.DistributedTask.Pipelines.Runtime;
using GitHub.DistributedTask.Pipelines.Validation;
using GitHub.DistributedTask.WebApi;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ProviderPhase : PhaseNode
    {
        public ProviderPhase()
        {
        }

        private ProviderPhase(ProviderPhase phaseToCopy)
            : base(phaseToCopy)
        {
        }

        /// <summary>
        /// Gets the phase type.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public override PhaseType Type => PhaseType.Provider;

        /// <summary>
        /// Gets or sets the environment target for this phase.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public EnvironmentDeploymentTarget EnvironmentTarget
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the provider for this phase.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Provider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the strategy for this phase.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<String, JToken> Strategy
        {
            get;
            set;
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

            var provider = context.PhaseProviders.FirstOrDefault(x => String.Equals(x.Provider, this.Provider, StringComparison.OrdinalIgnoreCase));
            if (provider == null)
            {
                result.Errors.Add(new PipelineValidationError($"'{this.Provider}' phase '{this.Name}' is not supported."));
            }
            else
            {
                var providerPhaseResult = provider.Validate(context, this);
                if (providerPhaseResult != null)
                {
                    foreach (var error in providerPhaseResult.Errors)
                    {
                        result.Errors.Add(error);
                    }

                    result.ReferencedResources.MergeWith(providerPhaseResult.ReferencedResources);

                    foreach (var endpointReference in providerPhaseResult.ReferencedResources.Endpoints)
                    {
                        var endpoint = context.ResourceStore.GetEndpoint(endpointReference);
                        if (endpoint == null)
                        {
                            result.UnauthorizedResources.AddEndpointReference(endpointReference);
                        }
                    }

                    foreach (var fileReference in providerPhaseResult.ReferencedResources.Files)
                    {
                        var file = context.ResourceStore.GetFile(fileReference);
                        if (file == null)
                        {
                            result.UnauthorizedResources.AddSecureFileReference(fileReference);
                        }
                    }

                    foreach (var queueReference in providerPhaseResult.ReferencedResources.Queues)
                    {
                        var queue = context.ResourceStore.GetQueue(queueReference);
                        if (queue == null)
                        {
                            result.UnauthorizedResources.AddAgentQueueReference(queueReference);
                        }
                    }

                    foreach (var variableReference in providerPhaseResult.ReferencedResources.VariableGroups)
                    {
                        var variableGroup = context.ResourceStore.GetVariableGroup(variableReference);
                        if (variableGroup == null)
                        {
                            result.UnauthorizedResources.AddVariableGroupReference(variableReference);
                        }
                    }
                }
            }

            if (!(this.Target is AgentQueueTarget agentQueueTarget) || agentQueueTarget.IsLiteral())
            {
                this.Target?.Validate(context, context.BuildOptions, result);
            }
        }
        public JobExecutionContext CreateJobContext(
            PhaseExecutionContext context,
            JobInstance jobInstance)
        {
            var jobContext = context.CreateJobContext(jobInstance);
            jobContext.Job.Definition.Id = jobContext.GetInstanceId();

            var options = new BuildOptions();
            var builder = new PipelineBuilder(context);
            var result = builder.GetReferenceResources(jobInstance.Definition.Steps.OfType<Step>().ToList(), jobInstance.Definition.Target);
            jobContext.ReferencedResources.MergeWith(result);

            // Update the execution context with the job-specific system variables
            UpdateJobContextVariablesFromJob(jobContext, jobInstance.Definition);

            return jobContext;
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public class ProviderPhaseRequest
    {
        [DataMember(IsRequired = true)]
        public Guid PlanId { get; set; }

        [DataMember(IsRequired = true)]
        public String PlanType { get; set; }

        [DataMember(IsRequired = true)]
        public Guid ServiceOwner { get; set; }

        [DataMember(IsRequired = true)]
        public String PhaseOrchestrationId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public ProviderPhase ProviderPhase { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public ProjectReference Project { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public TaskOrchestrationOwner Pipeline { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public TaskOrchestrationOwner Run { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public PipelineGraphNodeReference Stage { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public PipelineGraphNodeReference Phase { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public IDictionary<String, VariableValue> Variables { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public class PipelineGraphNodeReference
    {
        public PipelineGraphNodeReference()
        {
        }

        public PipelineGraphNodeReference(String id, String name, Int32 attempt = 0)
        {
            this.Id = id;
            this.Name = name;
            this.Attempt = attempt;
        }

        public PipelineGraphNodeReference(Guid id, String name, Int32 attempt = 0)
        {
            this.Id = id.ToString("D");
            this.Name = name;
            this.Attempt = attempt;
        }

        public PipelineGraphNodeReference(Int32 id, String name, Int32 attempt = 0)
        {
            this.Id = id.ToString();
            this.Name = name;
            this.Attempt = attempt;
        }

        [DataMember(IsRequired = true)]
        public String Id { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public String Name { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public Int32 Attempt { get; set; }
    }
}
