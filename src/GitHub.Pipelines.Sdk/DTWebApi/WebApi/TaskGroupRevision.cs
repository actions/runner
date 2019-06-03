using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class TaskGroupRevision
    {
        [DataMember]
        public Guid TaskGroupId { get; set; }

        [DataMember]
        public Int32 Revision { get; set; }

        [DataMember]
        public Int32 MajorVersion { get; set; }

        [DataMember]
        public IdentityRef ChangedBy { get; set; }

        [DataMember]
        public DateTime ChangedDate { get; set; }

        [DataMember]
        public AuditAction ChangeType { get; set; }

        [DataMember]
        public Int32 FileId { get; set; }

        [DataMember]
        public String Comment { get; set; }
    }
}
