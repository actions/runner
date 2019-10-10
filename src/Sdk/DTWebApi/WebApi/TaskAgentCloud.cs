using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class TaskAgentCloud
    {
        private TaskAgentCloud(TaskAgentCloud cloudToBeCloned)
        {
            this.Id = cloudToBeCloned.Id;
            this.AgentCloudId = cloudToBeCloned.AgentCloudId;
            this.Name = cloudToBeCloned.Name;
            this.AcquireAgentEndpoint = cloudToBeCloned.AcquireAgentEndpoint;
            this.ReleaseAgentEndpoint = cloudToBeCloned.ReleaseAgentEndpoint;
            this.SharedSecret = cloudToBeCloned.SharedSecret;
            this.Internal = cloudToBeCloned.Internal;

            if (cloudToBeCloned.GetAgentDefinitionEndpoint != null)
            {
                this.GetAgentDefinitionEndpoint = cloudToBeCloned.GetAgentDefinitionEndpoint;
            }

            if (cloudToBeCloned.GetAgentRequestStatusEndpoint != null)
            {
                this.GetAgentRequestStatusEndpoint = cloudToBeCloned.GetAgentRequestStatusEndpoint;
            }

            if (cloudToBeCloned.AcquisitionTimeout != null)
            {
                this.AcquisitionTimeout = cloudToBeCloned.AcquisitionTimeout;
            }
            
            if (cloudToBeCloned.GetAccountParallelismEndpoint != null)
            {
                this.GetAccountParallelismEndpoint = cloudToBeCloned.GetAccountParallelismEndpoint;
            }

            if (cloudToBeCloned.MaxParallelism != null)
            {
                this.MaxParallelism = cloudToBeCloned.MaxParallelism;
            }
        }

        public TaskAgentCloud()
        {
        }

        //Id is used for interacting with pool providers, AgentCloudId is internal Id

        [DataMember]
        public Guid Id
        {
            get;
            set;
        }

        [DataMember]
        public Int32 AgentCloudId
        {
            get;
            set;
        }

        [DataMember]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        ///  Gets or sets the type of the endpoint.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Type
        {
            get;
            set;
        }

        /// <summary>
        /// Signifies that this Agent Cloud is internal and should not be user-manageable
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean Internal
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String SharedSecret
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a AcquireAgentEndpoint using which a request can be made to acquire new agent
        /// </summary>
        [DataMember]
        public String AcquireAgentEndpoint
        {
            get;
            set;
        }

        [DataMember]
        public String ReleaseAgentEndpoint
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public String GetAgentDefinitionEndpoint
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public String GetAgentRequestStatusEndpoint
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public Int32? AcquisitionTimeout
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public String GetAccountParallelismEndpoint
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public Int32? MaxParallelism
        {
            get;
            set;
        }

        public TaskAgentCloud Clone()
        {
            return new TaskAgentCloud(this);
        }
    }
}
