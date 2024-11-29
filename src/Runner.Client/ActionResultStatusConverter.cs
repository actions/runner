using System.Runtime.Serialization;
using GitHub.DistributedTask.WebApi;

namespace Runner.Client
{
    public static class ActionResultStatusConverter {
        public static ActionResultStatus FromPipelines(TaskResult pipelineStatus) {
            ActionResultStatus result = ActionResultStatus.Failure;
            switch(pipelineStatus) {
                case TaskResult.Failed:
                case TaskResult.Abandoned:
                    result = ActionResultStatus.Failure;
                    break;
                case TaskResult.Canceled:
                    result = ActionResultStatus.Cancelled;
                    break;
                case TaskResult.Succeeded:
                case TaskResult.SucceededWithIssues:
                    result = ActionResultStatus.Success;
                    break;
                case TaskResult.Skipped:
                    result = ActionResultStatus.Skipped;
                    break;
            }
            return result;
        }
    }
}

