using GitHub.Services.Common;
using GitHub.Services.WebApi;
using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class TaskAttachment
    {
        internal TaskAttachment()
        { }

        internal TaskAttachment(String type, String name, ReferenceLinks links)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(type, "type");
            ArgumentUtility.CheckStringForNullOrEmpty(name, "name");
            this.Type = type;
            this.Name = name;
            this.m_links = links;
        }

        public TaskAttachment(String type, String name)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(type, "type");
            ArgumentUtility.CheckStringForNullOrEmpty(name, "name");
            this.Type = type;
            this.Name = name;
        }
        

        [DataMember]
        public String Type
        {
            get;
            internal set;
        }

        [DataMember]
        public String Name
        {
            get;
            internal set;
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

        [DataMember]
        public DateTime CreatedOn
        {
            get;
            internal set;
        }

        [DataMember]
        public DateTime LastChangedOn
        {
            get;
            internal set;
        }

        [DataMember]
        public Guid LastChangedBy
        {
            get;
            internal set;
        }

        [DataMember]
        public Guid TimelineId
        {
            get;
            set;
        }

        [DataMember]
        public Guid RecordId
        {
            get;
            set;
        }

        [DataMember(Name = "_links", EmitDefaultValue = false)]
        private ReferenceLinks m_links;
    }

    [GenerateAllConstants]
    public class CoreAttachmentType
    {
        public static readonly String Log = "DistributedTask.Core.Log";
        public static readonly String Summary = "DistributedTask.Core.Summary";
        public static readonly String FileAttachment = "DistributedTask.Core.FileAttachment";
        public static readonly String DiagnosticLog = "DistributedTask.Core.DiagnosticLog";
    }
}
