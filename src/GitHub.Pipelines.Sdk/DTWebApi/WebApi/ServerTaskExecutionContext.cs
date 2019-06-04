using System;
using System.ComponentModel;

namespace GitHub.DistributedTask.WebApi
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ServerTaskExecutionContext
    {
        public int CurrentSectionIndex { get; set; }

        public int HandlerInvocationCount { get; set; }

        public Boolean UseExistingTimelineRecord { get; set; }

        public Boolean HasMoreSections { get; set; }

        public Boolean UseNewOrchestrationIdentifierForGates { get; set; }
    }
}
