using System;
using System.Runtime.Serialization;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Common;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi {    
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskAgentSessionExpiredException", "Microsoft.TeamFoundation.DistributedTask.WebApi.TaskAgentSessionExpiredException, Microsoft.TeamFoundation.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskAgentSessionExpiredException : DistributedTaskException
    {
        public TaskAgentSessionExpiredException(String message)
            : base(message)
        {
        }

        public TaskAgentSessionExpiredException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskAgentSessionExpiredException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}