using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Runtime
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class PhaseInstance : GraphNodeInstance<PhaseNode>
    {
        public PhaseInstance()
        {
        }

        public PhaseInstance(String name)
            : this(name, TaskResult.Succeeded)
        {
        }

        public PhaseInstance(
            String name,
            Int32 attempt)
            : this(name, attempt, null, TaskResult.Succeeded)
        {
        }

        public PhaseInstance(PhaseNode phase)
            : this(phase, 1)
        {
        }

        public PhaseInstance(
            PhaseNode phase,
            Int32 attempt)
            : this(phase.Name, attempt, phase, TaskResult.Succeeded)
        {
        }

        public PhaseInstance(
            String name,
            TaskResult result)
            : this(name, 1, null, result)
        {
        }

        public PhaseInstance(
            String name,
            Int32 attempt,
            PhaseNode definition,
            TaskResult result)
            : base(name, attempt, definition, result)
        {
        }

        public static implicit operator PhaseInstance(String name)
        {
            return new PhaseInstance(name);
        }
    }
}
