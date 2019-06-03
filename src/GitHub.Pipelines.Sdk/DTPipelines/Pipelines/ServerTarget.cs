using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.DistributedTask.Pipelines.Runtime;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ServerTarget : PhaseTarget
    {
        public ServerTarget()
            : base(PhaseTargetType.Server)
        {
        }

        private ServerTarget(ServerTarget targetToClone)
            : base(targetToClone)
        {
            this.Execution = targetToClone.Execution?.Clone();
        }

        [DataMember(EmitDefaultValue = false)]
        public ParallelExecutionOptions Execution
        {
            get;
            set;
        }

        public override PhaseTarget Clone()
        {
            return new ServerTarget(this);
        }

        public override Boolean IsValid(TaskDefinition task)
        {
            return task.RunsOn.Contains(TaskRunsOnConstants.RunsOnServer, StringComparer.OrdinalIgnoreCase);
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
            var e = this.Execution ?? new ParallelExecutionOptions();
            var jobContext = e.CreateJobContext(
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

            jobContext.Variables[WellKnownDistributedTaskVariables.EnableAccessToken] = Boolean.TrueString;
            return jobContext;
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
            var execution = this.Execution ?? new ParallelExecutionOptions();
            var result = execution.Expand(
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
    }
}
