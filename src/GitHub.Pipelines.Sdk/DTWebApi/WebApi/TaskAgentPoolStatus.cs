using System;
using System.Runtime.Serialization;
using System.ComponentModel;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class TaskAgentPoolStatus : TaskAgentPoolReference
    {
        internal TaskAgentPoolStatus()
        {
        }

        public TaskAgentPoolStatus(Int32 id)
        {
            this.Id = id;
        }

        private TaskAgentPoolStatus(TaskAgentPoolStatus toClone) : base(toClone)
        {
            this.QueuedRequestCount = toClone.QueuedRequestCount;
            this.AssignedRequestCount = toClone.AssignedRequestCount;
            this.RunningRequestCount = toClone.RunningRequestCount;
        }

        /// <summary>
        /// Number of queued requests which are not assigned to any agents
        /// </summary>
        [DataMember]
        public Int32 QueuedRequestCount
        {
            get;
            set;
        }

        /// <summary>
        /// Number of requests queued and assigned to an agent. Not running yet.
        /// </summary>
        [DataMember]
        public Int32 AssignedRequestCount
        {
            get;
            set;
        }

        /// <summary>
        /// Number of currently running requests
        /// </summary>
        [DataMember]
        public Int32 RunningRequestCount
        {
            get;
            set;
        }

        public new TaskAgentPoolStatus Clone()
        {
            return new TaskAgentPoolStatus(this);
        }
    }
}
