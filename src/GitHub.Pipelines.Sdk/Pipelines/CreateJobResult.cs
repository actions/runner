using System.ComponentModel;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Runtime;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct CreateJobResult
    {
        public CreateJobResult(
            JobExecutionContext context, 
            Job job)
        {
            this.Job = job;
            this.Context = context;
        }

        public Job Job
        {
            get;
        }

        public JobExecutionContext Context
        {
            get;
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct CreateTaskResult
    {
        public CreateTaskResult(
            TaskStep task,
            TaskDefinition definition)
        {
            this.Task = task;
            this.Definition = definition;
        }

        public TaskStep Task
        {
            get;
        }

        public TaskDefinition Definition
        {
            get;
        }
    }
}
