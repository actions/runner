using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Services.Security
{
    [DataContract]
    public sealed class SetAccessControlEntriesInfo
    {
        public SetAccessControlEntriesInfo(
            String token,
            IEnumerable<AccessControlEntry> accessControlEntries,
            Boolean merge)
        {
            Token = token;
            Merge = merge;
            AccessControlEntries = new AccessControlEntriesCollection(accessControlEntries);
        }

        [DataMember]
        public String Token { get; private set; }

        [DataMember]
        public Boolean Merge { get; private set; }

        [DataMember]
        public AccessControlEntriesCollection AccessControlEntries { get; private set; }
    }
}
