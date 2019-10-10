using GitHub.Services.Common;
using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Identity
{
    //Technically speaking, this is poor REST
    //a PUT or POST to a service to create an entity should
    //contain that entity, in this case an IdentityScope
    //however this contains extra fields not in an IdentityScope
    [DataContract]
    public class CreateScopeInfo
    {
        public CreateScopeInfo()
        {
        }

        internal CreateScopeInfo(Guid parentScopeId, GroupScopeType scopeType, String scopeName, String adminGroupName, String adminGroupDescription, Guid creatorId)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(scopeName, "scopeName");
            ArgumentUtility.CheckStringForNullOrEmpty(adminGroupName, "adminGroupName");
            ArgumentUtility.CheckStringForNullOrEmpty(adminGroupDescription, "admingGroupDescription");

            ParentScopeId = parentScopeId;
            ScopeType = scopeType;
            ScopeName = scopeName;
            AdminGroupName = adminGroupName;
            AdminGroupDescription = adminGroupDescription;
            CreatorId = creatorId;
        }

        [DataMember]
        public Guid ParentScopeId { get; private set; }

        [DataMember]
        public GroupScopeType ScopeType { get; private set; }

        [DataMember]
        public String ScopeName { get; private set; }

        [DataMember]
        public String AdminGroupName { get; private set; }

        [DataMember]
        public String AdminGroupDescription { get; private set; }

        [DataMember(IsRequired=false, EmitDefaultValue=false)]
        public Guid CreatorId { get; private set; }

    }
}
