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

    public class MigrationEventArgs : EventArgs
    {
        public MigrationEventArgs(string source)
        {
            this.Source = source;
        }
        public string Source { get; private set; }
    }
}
