using System.ComponentModel;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class PipelineTrigger
    {
        public PipelineTrigger(PipelineTriggerType triggerType)
        {
            TriggerType = triggerType;
        }

        /// <summary>
        /// The type of the trigger.
        /// </summary>
        public PipelineTriggerType TriggerType
        {
            get;
            private set;
        }
    }
}
