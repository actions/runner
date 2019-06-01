using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using GroupScopeType = Microsoft.VisualStudio.Services.Identity.GroupScopeType;
using IdentityDescriptor = Microsoft.VisualStudio.Services.Identity.IdentityDescriptor;

namespace Microsoft.VisualStudio.Services.Graph.Client
{
    /// <summary>
    /// Container where a graph entity is defined (organization, project, team)
    /// </summary>
    [DataContract]
    public class GraphScope : GraphSubject
    {
        public override string SubjectKind => Constants.SubjectKind.Scope;

        /// <summary>
        /// The subject descriptor that references the administrators group for this scope. Only
        /// members of this group can change the contents of this scope or assign other users
        /// permissions to access this scope.
        /// </summary>
        public SubjectDescriptor AdministratorDescriptor { get; private set; }

        /// <summary>
        /// The subject descriptor that references the administrators group for this scope. Only
        /// members of this group can change the contents of this scope or assign other users
        /// permissions to access this scope.
        /// </summary>
        [DataMember(Name = "AdministratorDescriptor", IsRequired = false, EmitDefaultValue = false)]
        private string AdministratorString
        {
            get { return AdministratorDescriptor.ToString(); }
            set { AdministratorDescriptor = SubjectDescriptor.FromString(value); }
        }

        /// <summary>
        /// When true, this scope is also a securing host for one or more scopes.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool IsGlobal { get; private set; }

        /// <summary>
        /// The subject descriptor of the parent scope.
        /// </summary>
        public SubjectDescriptor ParentDescriptor { get; private set; }

        /// <summary>
        /// The subject descriptor for the closest account or organization in the 
        /// ancestor tree of this scope.
        /// </summary>
        [DataMember(Name = "ParentDescriptor", IsRequired = false, EmitDefaultValue = false)]
        private string ParentDescriptorString
        {
            get { return ParentDescriptor.ToString(); }
            set { ParentDescriptor = SubjectDescriptor.FromString(value); }
        }

        /// <summary>
        /// The subject descriptor for the containing organization in the ancestor tree 
        /// of this scope.
        /// </summary>
        public SubjectDescriptor SecuringHostDescriptor { get; private set; }

        /// <summary>
        /// The subject descriptor for the containing organization in the ancestor tree 
        /// of this scope.
        /// </summary>
        [DataMember(Name = "SecuringHostDescriptor", IsRequired = false, EmitDefaultValue = false)]
        private string SecuringHostDescriptorString
        {
            get { return SecuringHostDescriptor.ToString(); }
            set { SecuringHostDescriptor = SubjectDescriptor.FromString(value); }
        }

        /// <summary>
        /// The type of this scope. Typically ServiceHost or TeamProject.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public GroupScopeType ScopeType { get; private set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal GraphScope(
            string origin,
            string originId,
            SubjectDescriptor descriptor,
            IdentityDescriptor legacyDescriptor,
            string displayName,
            ReferenceLinks links,
            string url,
            SubjectDescriptor administratorDescriptor,
            bool isGlobal,
            SubjectDescriptor parentDescriptor,
            SubjectDescriptor securingHostDescriptor,
            GroupScopeType scopeType = GroupScopeType.Generic)
            : base(origin, originId, descriptor, legacyDescriptor, displayName, links, url)
        {
            AdministratorDescriptor = administratorDescriptor;
            IsGlobal = isGlobal;
            ParentDescriptor = parentDescriptor;
            SecuringHostDescriptor = securingHostDescriptor;
            ScopeType = scopeType;
        }

        // this is how we replace/overwrite parameters and create a new object
        // and keep our internal objects immutable
        internal GraphScope(
            GraphScope scope,
            string origin = null,
            string originId = null,
            SubjectDescriptor? descriptor = null,
            IdentityDescriptor legacyDescriptor = null,
            string displayName = null,
            ReferenceLinks links = null,
            string url = null,
            SubjectDescriptor? administrator = null,
            bool? isGlobal = null,
            SubjectDescriptor? parentDescriptor = null,
            SubjectDescriptor? securingHostDescriptor = null,
            GroupScopeType? scopeType = GroupScopeType.Generic)
            : this(origin ?? scope?.Origin,
                   originId ?? scope?.OriginId,
                   descriptor ?? scope?.Descriptor ?? default(SubjectDescriptor),
                   legacyDescriptor ?? scope?.LegacyDescriptor ?? default(IdentityDescriptor),
                   displayName ?? scope?.DisplayName,
                   links ?? scope?.Links,
                   url ?? scope?.Url,
                   administrator ?? scope?.AdministratorDescriptor ?? default(SubjectDescriptor),
                   isGlobal ?? scope?.IsGlobal ?? default(bool),
                   parentDescriptor ?? scope?.ParentDescriptor ?? default(SubjectDescriptor),
                   securingHostDescriptor ?? scope?.SecuringHostDescriptor ?? default(SubjectDescriptor),
                   scopeType ?? scope?.ScopeType ?? default(GroupScopeType))
        { }

        // only for serialization
        protected GraphScope() { }
    }
}
