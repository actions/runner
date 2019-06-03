using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Services.Identity
{
    [DataContract]
    public class CreateGroupsInfo
    {
        public CreateGroupsInfo()
        {
        }

        public CreateGroupsInfo(Guid scopeId, IList<Identity> groups)
        {
            this.ScopeId = scopeId;
            this.Groups = new List<Identity>(groups);
        }

        [DataMember]
        public Guid ScopeId { get; private set; }

        [DataMember]
        public List<Identity> Groups { get; private set; }
    }
}
