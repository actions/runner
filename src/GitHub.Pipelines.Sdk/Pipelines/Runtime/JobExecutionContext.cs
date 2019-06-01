using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Tokens;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.ContextData;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Runtime
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class JobExecutionContext : PipelineExecutionContext
    {
        public JobExecutionContext(
            PipelineState state,
            IPipelineIdGenerator idGenerator = null)
            : base(null, null, null, null, null, state, idGenerator)
        {
        }

        public JobExecutionContext(
            PhaseExecutionContext context,
            JobInstance job,
            IDictionary<String, VariableValue> variables,
            Int32 positionInPhase = default,
            Int32 totalJobsInPhase = default,
            IDictionary<String, PipelineContextData> data = default)
            : base(context)
        {
            this.Stage = context.Stage;
            this.Phase = context.Phase;
            this.Job = job;

            // Make sure the identifier is properly set
            this.Job.Identifier = this.IdGenerator.GetJobIdentifier(this.Stage?.Name, this.Phase.Name, this.Job.Name);

            if (job.Definition?.Variables?.Count > 0)
            {
                SetUserVariables(job.Definition.Variables.OfType<Variable>());
            }

            SetSystemVariables(variables);

            // Add the attempt information into the context
            var systemVariables = new List<Variable>
            {
                new Variable
                {
                    Name = WellKnownDistributedTaskVariables.JobIdentifier,
                    Value = job.Identifier
                },
                new Variable
                {
                    Name = WellKnownDistributedTaskVariables.JobAttempt,
                    Value = job.Attempt.ToString()
                },
            };

            if (positionInPhase != default)
            {
                systemVariables.Add(new Variable
                {
                    Name = WellKnownDistributedTaskVariables.JobPositionInPhase,
                    Value = positionInPhase.ToString()
                });
            }

            if (totalJobsInPhase != default)
            {
                systemVariables.Add(new Variable
                {
                    Name = WellKnownDistributedTaskVariables.TotalJobsInPhase,
                    Value = totalJobsInPhase.ToString()
                });
            }

            SetSystemVariables(systemVariables);

            if (String.IsNullOrEmpty(this.ExecutionOptions.SystemTokenScope) &&
                this.Variables.TryGetValue(WellKnownDistributedTaskVariables.AccessTokenScope, out VariableValue tokenScope))
            {
                this.ExecutionOptions.SystemTokenScope = tokenScope?.Value;
            }

            Data = data;
        }

        public StageInstance Stage
        {
            get;
        }

        public PhaseInstance Phase
        {
            get;
        }

        public JobInstance Job
        {
            get;
        }

        public IDictionary<String, PipelineContextData> Data
        {
            get;
        }

        internal override String GetInstanceName()
        {
            return this.IdGenerator.GetJobInstanceName(this.Stage?.Name, this.Phase.Name, this.Job.Name, this.Job.Attempt);
        }
    }
}
