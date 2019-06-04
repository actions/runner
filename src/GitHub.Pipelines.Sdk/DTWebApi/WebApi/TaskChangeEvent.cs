using GitHub.Services.WebApi;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
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
