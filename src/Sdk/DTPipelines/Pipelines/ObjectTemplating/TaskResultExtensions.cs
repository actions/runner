using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines.ObjectTemplating
{
    public static class TaskResultExtensions
    {
        public static PipelineContextData ToContextData(this TaskResult result)
        {
            switch (result)
            {
                case TaskResult.Succeeded:
                case TaskResult.SucceededWithIssues:
                    return new StringContextData(PipelineTemplateConstants.Success);
                case TaskResult.Failed:
                case TaskResult.Abandoned:
                    return new StringContextData(PipelineTemplateConstants.Failure);
                case TaskResult.Canceled:
                    return new StringContextData(PipelineTemplateConstants.Cancelled);
                case TaskResult.Skipped:
                    return new StringContextData(PipelineTemplateConstants.Skipped);
            }

            return null;
        }

        public static PipelineContextData ToContextData(this TaskResult? result)
        {
            if (result.HasValue)
            {
                return result.Value.ToContextData();
            }

            return null;
        }
    }
}
