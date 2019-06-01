using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Identity
{
    [DataContract]
    public class IdentitySnapshot
    {
        public IdentitySnapshot()
        {
        }

        public IdentitySnapshot(Guid scopeId)
        {
            ScopeId = scopeId;
            Scopes = new List<IdentityScope>();
            Groups = new List<Identity>();
            Memberships = new List<GroupMembership>();
            IdentityIds = new List<Guid>();
        }

        [DataMember]
        public Guid ScopeId
        {
            get;
            set;
        }

        [DataMember]
        public List<IdentityScope> Scopes
        {
            get;
            set;
        }

        [DataMember]
        public List<Identity> Groups
        {
            get;
            set;
        }

        [DataMember]
        public List<GroupMembership> Memberships
        {
            get;
            set;
        }

        [DataMember]
        public List<Guid> IdentityIds
        {
            get;
            set;
        }

        public IdentitySnapshot Clone()
        {
            return new IdentitySnapshot()
            {
                ScopeId = this.ScopeId,
                Scopes = this.Scopes?.Where(x => x != null).Select(x => x.Clone()).ToList(),
                Groups = this.Groups?.Where(x => x != null).Select(x => x.Clone()).ToList(),
                Memberships = this.Memberships?.Where(x => x != null).Select(x => x.Clone()).ToList(),
                IdentityIds = this.IdentityIds.ToList(),
            };
        }

        public override string ToString()
        {
            return string.Format("[ScopeId = {0}, Scopes={1}, Groups={2}, Memberships={3}, Identities={4}]",
                ScopeId, 
                Scopes?.Count, 
                Groups?.Count, 
                Memberships?.Count, 
                IdentityIds?.Count);
        }
    }
}
