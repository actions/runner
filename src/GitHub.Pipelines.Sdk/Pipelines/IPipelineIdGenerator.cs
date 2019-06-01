using System;
using System.ComponentModel;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IPipelineIdGenerator
    {
        Guid GetInstanceId(params String[] segments);

        String GetInstanceName(params String[] segments);

        String GetStageIdentifier(String stageName);

        Guid GetStageInstanceId(String stageName, Int32 attempt);

        String GetStageInstanceName(String stageName, Int32 attempt);

        String GetPhaseIdentifier(String stageName, String phaseName);

        Guid GetPhaseInstanceId(String stageName, String phaseName, Int32 attempt);

        String GetPhaseInstanceName(String stageName, String phaseName, Int32 attempt);

        String GetJobIdentifier(String stageName, String phaseName, String jobName);

        Guid GetJobInstanceId(String stageName, String phaseName, String jobName, Int32 attempt);

        String GetJobInstanceName(String stageName, String phaseName, String jobName, Int32 attempt);

        Guid GetTaskInstanceId(String stageName, String phaseName, String jobName, Int32 jobAttempt, String name3);

        String GetTaskInstanceName(String stageName, String phaseName, String jobName, Int32 jobAttempt, String name);
    }
}
