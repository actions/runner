using System.ComponentModel;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Runtime
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class JobAttempt
    {
        public JobInstance Job
        {
            get;
            set;
        }
    }
}
