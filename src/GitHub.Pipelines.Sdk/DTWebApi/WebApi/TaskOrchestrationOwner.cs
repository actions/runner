using GitHub.Services.WebApi;
using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class TaskOrchestrationOwner : ICloneable
    {
        public TaskOrchestrationOwner()
        {
        }

        private TaskOrchestrationOwner(TaskOrchestrationOwner ownerToBeCloned)
        {
            this.Id = ownerToBeCloned.Id;
            this.Name = ownerToBeCloned.Name;
            this.m_links = ownerToBeCloned.Links.Clone();
        }

        [DataMember]
        public Int32 Id
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

        public ReferenceLinks Links
        {
            get
            {
                if (m_links == null)
                {
                    m_links = new ReferenceLinks();
                }
                return m_links;
            }
        }

        public TaskOrchestrationOwner Clone()
        {
            return new TaskOrchestrationOwner(this);
        }

        Object ICloneable.Clone()
        {
            return this.Clone();
        }

        [DataMember(Name = "_links")]
        private ReferenceLinks m_links;
    }
}
