using GitHub.Services.Common;
using GitHub.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    [ServiceEventObject]
    public class SecureFileEvent
    {
        public SecureFileEvent(
            String eventType,
            IEnumerable<SecureFile> secureFiles,
            Guid projectId)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(eventType, "eventType");
            ArgumentUtility.CheckForNull(secureFiles, "secureFiles");
            ArgumentUtility.CheckForEmptyGuid(projectId, "projectId");

            this.EventType = eventType;
            this.SecureFiles = secureFiles;
            this.ProjectId = projectId;
        }

        [DataMember]
        public String EventType
        {
            get;
            set;
        }

        [DataMember]
        public IEnumerable<SecureFile> SecureFiles
        {
            get;
            set;
        }

        [DataMember]
        public Guid ProjectId
        {
            get;
            set;
        }
    }
}
