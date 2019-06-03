using System.ComponentModel;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum PipelineTriggerType
    {
        /// <summary>
        /// A pipeline should be started for each changeset.
        /// </summary>
        ContinuousIntegration = 2,

        /// <summary>
        /// A pipeline should be triggered when a GitHub pull request is created or updated.
        /// </summary>
        PullRequest = 64,
    }
}
