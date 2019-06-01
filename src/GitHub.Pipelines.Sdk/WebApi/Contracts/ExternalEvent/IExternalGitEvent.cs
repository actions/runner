using System;

namespace Microsoft.VisualStudio.Services.ExternalEvent
{
    /// <summary>
    /// This interface is used to group external Git events (push, pull request).
    /// </summary>
    public interface IExternalGitEvent
    {
        /// <summary>
        /// The Id of this external event. Can be used to tie all subsequent actions to this payload.
        /// </summary>
        String PipelineEventId
        {
            get;
            set;
        }
    }
}
