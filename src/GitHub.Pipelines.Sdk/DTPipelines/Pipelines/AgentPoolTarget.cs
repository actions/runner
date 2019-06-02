using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Runtime;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Validation;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Newtonsoft.Json.Linq;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class AgentPoolTarget : PhaseTarget
    {
        public AgentPoolTarget()
           : base(PhaseTargetType.Pool)
        {
        }

        private AgentPoolTarget(AgentPoolTarget targetToClone)
            : base(targetToClone)
        {
            this.Pool = targetToClone.Pool?.Clone();
            

            if (targetToClone.AgentSpecification != null)
            {
                this.AgentSpecification = new JObject(targetToClone.AgentSpecification);
            }

            if (targetToClone.m_agentIds?.Count > 0)
            {
                this.m_agentIds = targetToClone.m_agentIds;
            }
        }

        /// <summary>
        /// Gets or sets the target pool from which agents will be selected.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public AgentPoolReference Pool
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public JObject AgentSpecification
        {
            get;
            set;
        }

        /// <summary>
        /// Gets agent Ids filter on which deployment should be done.
        /// </summary>
        public List<Int32> AgentIds
        {
            get
            {
                if (m_agentIds == null)
                {
                    m_agentIds = new List<Int32>();
                }
                return m_agentIds;
            }
        }

        public override PhaseTarget Clone()
        {
            return new AgentPoolTarget(this);
        }

        public override Boolean IsValid(TaskDefinition task)
        {
            ArgumentUtility.CheckForNull(task, nameof(task));
            return task.RunsOn.Contains(TaskRunsOnConstants.RunsOnAgent, StringComparer.OrdinalIgnoreCase);
        }

        internal override void Validate(
            IPipelineContext context,
            BuildOptions buildOptions,
            ValidationResult result,
            IList<Step> steps,
            ISet<Demand> taskDemands)
        {
            // validate pool
            Int32 poolId = 0;
            String poolName = null;
            var pool = this.Pool;
            if (pool != null)
            {
                poolId = pool.Id;
                poolName = pool.Name?.GetValue(context)?.Value;
            }

            if (poolId == 0 && String.IsNullOrEmpty(poolName) && buildOptions.ValidateResources)
            {
                result.Errors.Add(new PipelineValidationError(PipelineStrings.QueueNotDefined()));
            }
            else
            {
                // we have a valid queue. record the reference
                result.AddPoolReference(poolId, poolName);

                // Attempt to resolve the queue using any identifier specified. We will look up by either ID
                // or name and the ID is preferred since it is immutable and more specific.
                if (buildOptions.ValidateResources)
                {
                    TaskAgentPool taskAgentPool = null;
                    var resourceStore = context.ResourceStore;
                    if (resourceStore != null)
                    {
                        if (poolId != 0)
                        {
                            taskAgentPool = resourceStore.GetPool(poolId);
                            if (taskAgentPool == null)
                            {
                                result.UnauthorizedResources.Pools.Add(new AgentPoolReference { Id = poolId });
                                result.Errors.Add(new PipelineValidationError(PipelineStrings.QueueNotFound(poolId)));
                            }
                        }
                        else if (!String.IsNullOrEmpty(poolName))
                        {
                            taskAgentPool = resourceStore.GetPool(poolName);
                            if (taskAgentPool == null)
                            {
                                result.UnauthorizedResources.Pools.Add(new AgentPoolReference { Name = poolName });
                                result.Errors.Add(new PipelineValidationError(PipelineStrings.QueueNotFound(poolName)));
                            }
                        }
                    }

                    // Store the resolved values inline to the resolved resource for this validation run
                    if (taskAgentPool != null)
                    {
                        this.Pool.Id = taskAgentPool.Id;
                        this.Pool.Name = taskAgentPool.Name;
                    }
                }
            }
        }

        internal override JobExecutionContext CreateJobContext(PhaseExecutionContext context, string jobName, int attempt, bool continueOnError, int timeoutInMinutes, int cancelTimeoutInMinutes, IJobFactory jobFactory)
        {
            throw new NotSupportedException(nameof(AgentPoolTarget));
        }

        internal override ExpandPhaseResult Expand(PhaseExecutionContext context, bool continueOnError, int timeoutInMinutes, int cancelTimeoutInMinutes, IJobFactory jobFactory, JobExpansionOptions options)
        {
            throw new NotSupportedException(nameof(AgentPoolTarget));
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_agentIds?.Count == 0)
            {
                m_agentIds = null;
            }
        }

        [DataMember(Name = "AgentIds", EmitDefaultValue = false)]
        private List<Int32> m_agentIds;
    }
}
