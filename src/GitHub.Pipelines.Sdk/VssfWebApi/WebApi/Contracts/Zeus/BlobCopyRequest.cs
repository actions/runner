using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;

namespace GitHub.Services.Zeus
{
    public enum BlobCopyRequestStatus
    {
        Created = 0,
        Running = 1,
        Failed = 2,
        Completed = 3
    }

    [DataContract]
    public class BlobCopyRequest
    {
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public int RequestId { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid JobId { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String SourceStorageAccount { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String TargetStorageAccount { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Containers { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public int ContainersCopied { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public BlobCopyRequestStatus Status { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string StatusMessage { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime? StartTime { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime? QueuedTime { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime? EndTime { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool CopyOnlyGuids { get; set; }
        public override string ToString()
        {
            // Intentionally not writing connection string.
            return String.Format(
                CultureInfo.InvariantCulture,
                @"BlobCopyRequest
[
    RequestId:                {0}
    JobId:                 {1}    
    Containers:            {2}
    ContainersCopied:      {3}
    QueuedTime:            {4}
    StartTime:             {5}
    EndTime:               {6}
    Status:                {7}
    StatusMessage:         {8}
    CopyOnlyGuids:         {9}
]",
                RequestId,
                JobId,
                Containers,
                ContainersCopied,
                QueuedTime,
                StartTime,
                EndTime,
                Status,
                StatusMessage,
                CopyOnlyGuids);
        }
    }
}
