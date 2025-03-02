using System.Collections.Generic;
using System.Threading;

namespace Runner.Server.Services
{
    public class WorkflowState
    {
        public CancellationTokenSource Cancel;
        public CancellationTokenSource ForceCancel;
        public Dictionary<string, TaskMetaData> TasksByNameAndVersion;
    }
}
