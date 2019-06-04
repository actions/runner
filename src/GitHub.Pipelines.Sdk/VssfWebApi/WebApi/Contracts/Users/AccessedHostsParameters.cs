using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.Services.Users
{
    /// <summary>
    /// Request to update a user's accessed hosts.
    /// </summary>
    [DataContract]
    public class AccessedHostsParameters
    {
        public AccessedHostsParameters()
        {
        }

        public AccessedHostsParameters(SubjectDescriptor userDescriptor, IList<AccessedHost> accessedHosts)
        {
            UserDescriptor = userDescriptor;
            AccessedHosts = accessedHosts;
        }

        [DataMember]
        public SubjectDescriptor UserDescriptor { get; set; }

        [DataMember]
        public IList<AccessedHost> AccessedHosts { get; set; }
    }

    /// <summary>
    /// Host accessed by a user.
    /// </summary>
    [DataContract]
    public class AccessedHost
    {
        public AccessedHost()
        {
        }

        public AccessedHost(Guid hostId, DateTime accessTime)
        {
            HostId = hostId;
            AccessTime = accessTime;
        }

        [DataMember]
        public Guid HostId { get; set; }

        [DataMember]
        public DateTime AccessTime { get; set; }
    }
}
