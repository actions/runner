using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.DistributedTask.Pipelines.Runtime;
using GitHub.DistributedTask.Pipelines.Validation;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    internal class DeploymentGroupTarget : PhaseTarget
    {
        public DeploymentGroupTarget()
             : base(PhaseTargetType.DeploymentGroup)
        {
        }

        private DeploymentGroupTarget(DeploymentGroupTarget targetToClone)
            : base(targetToClone)
        {
            this.DeploymentGroup = targetToClone.DeploymentGroup?.Clone();
            this.Execution = targetToClone.Execution?.Clone();

            if (targetToClone.m_tags != null && targetToClone.m_tags.Count > 0)
            {
                m_tags = new HashSet<String>(targetToClone.m_tags, StringComparer.OrdinalIgnoreCase);
            }
        }

        [DataMember]
        public DeploymentGroupReference DeploymentGroup
        {
            get;
            set;
        }

        public ISet<String> Tags
        {
            get
            {
                if (m_tags == null)
                {
                    m_tags = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
                }
                return m_tags;
            }
        }

        /// <summary>
        /// Gets targets Ids filter on which deployment should be done.
        /// </summary>
        public List<Int32> TargetIds
        {
            get
            {
                if (m_targetIds == null)
                {
                    m_targetIds = new List<Int32>();
                }
                return m_targetIds;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public DeploymentExecutionOptions Execution
        {
            get;
            set;
        }

        public override PhaseTarget Clone()
        {
            return new DeploymentGroupTarget(this);
        }

        public override Boolean IsValid(TaskDefinition task)
        {
            return task.RunsOn.Contains(TaskRunsOnConstants.RunsOnDeploymentGroup, StringComparer.OrdinalIgnoreCase);
        }

        internal override void Validate(
            IPipelineContext context,
            BuildOptions buildOptions,
            ValidationResult result,
            IList<Step> steps,
            ISet<Demand> taskDemands)
        {
            this.Execution?.Validate(context, result);
        }

        internal override JobExecutionContext CreateJobContext(
            PhaseExecutionContext context,
            String jobName,
            Int32 attempt,
            Boolean continueOnError,
            Int32 timeoutInMinutes,
            Int32 cancelTimeoutInMinutes,
            IJobFactory jobFactory)
        {
            context.Trace?.EnterProperty("CreateJobContext");
            var result = new ParallelExecutionOptions().CreateJobContext(
                context,
                jobName,
                attempt,
                null,
                null,
                continueOnError,
                timeoutInMinutes,
                cancelTimeoutInMinutes,
                jobFactory);
            context.Trace?.LeaveProperty("CreateJobContext");
            return result;
        }

        internal override ExpandPhaseResult Expand(
            PhaseExecutionContext context,
            Boolean continueOnError,
            Int32 timeoutInMinutes,
            Int32 cancelTimeoutInMinutes,
            IJobFactory jobFactory,
            JobExpansionOptions options)
        {
            context.Trace?.EnterProperty("Expand");
            var result = new ParallelExecutionOptions().Expand(
                context: context,
                container: null,
                sidecarContainers: null,
                continueOnError: continueOnError,
                timeoutInMinutes: timeoutInMinutes,
                cancelTimeoutInMinutes: cancelTimeoutInMinutes,
                jobFactory: jobFactory,
                options: options);
            context.Trace?.LeaveProperty("Expand");
            return result;
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_tags?.Count == 0)
            {
                m_tags = null;
            }

            if (m_targetIds?.Count == 0)
            {
                m_targetIds = null;
            }
        }

        [DataMember(Name = "Tags", EmitDefaultValue = false)]
        private ISet<String> m_tags;

        [DataMember(Name = "TargetIds")]
        private List<Int32> m_targetIds;
    }
}
