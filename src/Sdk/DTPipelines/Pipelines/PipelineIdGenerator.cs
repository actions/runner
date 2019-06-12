using System;
using System.ComponentModel;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PipelineIdGenerator : IPipelineIdGenerator
    {
        public PipelineIdGenerator(Boolean preserveCase = false)
        {
            m_preserveCase = preserveCase;
        }

        public Guid GetInstanceId(params String[] segments)
        {
            return PipelineUtilities.GetInstanceId(GetInstanceName(segments), m_preserveCase);
        }

        public String GetInstanceName(params String[] segments)
        {
            return PipelineUtilities.GetInstanceName(segments);
        }

        public String GetStageIdentifier(String stageName)
        {
            return PipelineUtilities.GetStageIdentifier(stageName);
        }

        public Guid GetStageInstanceId(
            String stageName,
            Int32 attempt)
        {
            return PipelineUtilities.GetStageInstanceId(stageName, attempt, m_preserveCase);
        }

        public String GetStageInstanceName(
            String stageName,
            Int32 attempt)
        {
            return PipelineUtilities.GetStageInstanceName(stageName, attempt);
        }

        public String GetPhaseIdentifier(
            String stageName,
            String phaseName)
        {
            return PipelineUtilities.GetPhaseIdentifier(stageName, phaseName);
        }

        public Guid GetPhaseInstanceId(
            String stageName,
            String phaseName,
            Int32 attempt)
        {
            return PipelineUtilities.GetPhaseInstanceId(stageName, phaseName, attempt, m_preserveCase);
        }

        public String GetPhaseInstanceName(
            String stageName,
            String phaseName,
            Int32 attempt)
        {
            return PipelineUtilities.GetPhaseInstanceName(stageName, phaseName, attempt);
        }

        public String GetJobIdentifier(
            String stageName,
            String phaseName,
            String jobName)
        {
            return PipelineUtilities.GetJobIdentifier(stageName, phaseName, jobName);
        }

        public Guid GetJobInstanceId(
            String stageName,
            String phaseName,
            String jobName,
            Int32 attempt)
        {
            return PipelineUtilities.GetJobInstanceId(stageName, phaseName, jobName, attempt, m_preserveCase);
        }

        public String GetJobInstanceName(
            String stageName,
            String phaseName,
            String jobName,
            Int32 attempt)
        {
            return PipelineUtilities.GetJobInstanceName(stageName, phaseName, jobName, attempt);
        }

        public String GetTaskInstanceName(
            String stageName,
            String phaseName,
            String jobName,
            Int32 jobAttempt,
            String taskName)
        {
            return PipelineUtilities.GetTaskInstanceName(stageName, phaseName, jobName, jobAttempt, taskName);
        }

        public Guid GetTaskInstanceId(
            String stageName,
            String phaseName,
            String jobName,
            Int32 jobAttempt,
            String taskName)
        {
            return PipelineUtilities.GetTaskInstanceId(stageName, phaseName, jobName, jobAttempt, taskName, m_preserveCase);
        }

        private Boolean m_preserveCase;
    }
}
