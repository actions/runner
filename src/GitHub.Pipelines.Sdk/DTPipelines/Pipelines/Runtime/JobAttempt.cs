using System.ComponentModel;

namespace GitHub.DistributedTask.Pipelines.Runtime
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
