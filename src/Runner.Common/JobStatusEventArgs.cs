using System;
using GitHub.DistributedTask.WebApi;

namespace GitHub.Runner.Common
{
    public class JobStatusEventArgs : EventArgs
    {
        public JobStatusEventArgs(TaskAgentStatus status)
        {
            this.Status = status;
        }
        public TaskAgentStatus Status { get; private set; }
    }
}
