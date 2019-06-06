using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.Services.Identity
{
    [DataContract]
    public class IdentityScope
    {
        internal IdentityScope()
        {
        }

        //Copy-Constructor
        internal IdentityScope(IdentityScope other)
            : this(other.Id, other.Name)
        {
            Administrators = other.Administrators == null ? null : new IdentityDescriptor(other.Administrators);
            IsActive = other.IsActive;
            IsGlobal = other.IsGlobal;
            LocalScopeId = other.LocalScopeId;
            ParentId = other.ParentId;
            ScopeType = other.ScopeType;
            SecuringHostId = other.SecuringHostId;
        }

        //Constructor used for the rename operation
        internal IdentityScope(Guid id, String name)
        {
            ArgumentUtility.CheckForEmptyGuid(id, "id");
            ArgumentUtility.CheckStringForNullOrEmpty(name, "name");
            this.Id = id;
            this.Name = name;
        }

        [DataMember(IsRequired=true)]
        public Guid Id
        {
            get;

            internal set;
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        internal Guid LocalScopeId
        {
            get;

            set;
        }

        [DataMember(IsRequired=false, EmitDefaultValue=false)]
        public Guid ParentId
        {
            get;

            internal set;
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public GroupScopeType ScopeType
        {
            get;

            internal set;
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public IdentityDescriptor Administrators
        {
            get;

            internal set;
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Boolean IsGlobal
        {
            get;
            internal set;
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid SecuringHostId
        {
            get;

            internal set;
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Boolean IsActive
        {
            get;

            internal set;
        }

        public IdentityScope Clone()
        {
            return new IdentityScope(this);
        }

        public override string ToString()
        {
            return $"[Id={Id}, Name={Name}, LocalScopeId={LocalScopeId}, ParentId={ParentId}, ScopeType={ScopeType}, SecuringHostId={SecuringHostId}, Administrators={Administrators}, IsActive={IsActive}, IsGlobal={IsGlobal}]";
        }

        private SubjectDescriptor subjectDescriptor;

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public SubjectDescriptor SubjectDescriptor
        {
            get
            {
                if (subjectDescriptor == default(SubjectDescriptor))
                {
                    subjectDescriptor = new SubjectDescriptor(Graph.Constants.SubjectType.GroupScopeType, Id.ToString());
                }

                return subjectDescriptor;
            }
        }
    }

    [CollectionDataContract(Name = "Scopes", ItemName = "Scope")]
    public class IdentityScopeCollection : List<IdentityScope>
    {
        public IdentityScopeCollection()
        {
        }

        public IdentityScopeCollection(IList<IdentityScope> source)
            : base(source)
        {
        }
    }
}
