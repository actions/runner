using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    [KnownType(typeof(TaskOrchestrationContainer))]
    [KnownType(typeof(TaskOrchestrationJob))]
    [JsonConverter(typeof(TaskOrchestrationItemJsonConverter))]
    public abstract class TaskOrchestrationItem
    {
        protected TaskOrchestrationItem(TaskOrchestrationItemType itemType)
        {
            this.ItemType = itemType;
        }

        [DataMember]
        public TaskOrchestrationItemType ItemType
        {
            get;
            private set;
        }
    }
}
