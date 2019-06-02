using Microsoft.VisualStudio.Services.WebApi;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    [ServiceEventObject]
    public class TaskChangeEvent
    {
        public TaskChangeEvent()
        {
        }
    }
}
