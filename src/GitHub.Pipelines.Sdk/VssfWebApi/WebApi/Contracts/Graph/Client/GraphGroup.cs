using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using IdentityDescriptor = Microsoft.VisualStudio.Services.Identity.IdentityDescriptor;

namespace Microsoft.VisualStudio.Services.Graph.Client
{
    /// <summary>
    /// Graph group entity
    /// </summary>
    [DataContract]
    public class GraphGroup : GraphMember
    {
        public override string SubjectKind => Constants.SubjectKind.Group;

        /// <summary>
        /// A short phrase to help human readers disambiguate groups with similar names
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string Description { get; private set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false), EditorBrowsable(EditorBrowsableState.Never), ClientInternalUseOnly]
        internal string SpecialType { get; private set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual bool ShouldSerializeSpecialType() => ShoudSerializeInternals;

        [DataMember(IsRequired = false, EmitDefaultValue = false), EditorBrowsable(EditorBrowsableState.Never), ClientInternalUseOnly]
        internal Guid ScopeId { get; private set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual bool ShouldSerializeScopeId() => ShoudSerializeInternals;

        [DataMember(IsRequired = false, EmitDefaultValue = false), EditorBrowsable(EditorBrowsableState.Never), ClientInternalUseOnly]
        internal string ScopeType { get; private set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual bool ShouldSerializeScopeType() => ShoudSerializeInternals;

        [DataMember(IsRequired = false, EmitDefaultValue = false), EditorBrowsable(EditorBrowsableState.Never), ClientInternalUseOnly]
        internal string ScopeName { get; private set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual bool ShouldSerializeScopeName() => ShoudSerializeInternals;

        [DataMember(IsRequired = false, EmitDefaultValue = false), EditorBrowsable(EditorBrowsableState.Never), ClientInternalUseOnly]
        internal Guid LocalScopeId { get; private set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual bool ShouldSerializeLocalScopeId() => ShoudSerializeInternals;

        [DataMember(IsRequired = false, EmitDefaultValue = false), EditorBrowsable(EditorBrowsableState.Never), ClientInternalUseOnly]
        internal Guid SecuringHostId { get; private set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual bool ShouldSerializeSecuringHostId() => ShoudSerializeInternals;

        [DataMember(IsRequired = false, EmitDefaultValue = false), EditorBrowsable(EditorBrowsableState.Never), ClientInternalUseOnly]
        internal bool IsRestrictedVisible { get; private set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual bool ShouldSerializeIsRestrictedVisible() => ShoudSerializeInternals;

        [DataMember(IsRequired = false, EmitDefaultValue = false), EditorBrowsable(EditorBrowsableState.Never), ClientInternalUseOnly]
        internal bool IsCrossProject { get; private set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual bool ShouldSerializeIsIsCrossProject() => ShoudSerializeInternals;

        [DataMember(IsRequired = false, EmitDefaultValue = false), EditorBrowsable(EditorBrowsableState.Never), ClientInternalUseOnly]
        internal bool IsGlobalScope { get; private set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual bool ShouldSerializeIsGlobalScope() => ShoudSerializeInternals;

        [DataMember(IsRequired = false, EmitDefaultValue = false), EditorBrowsable(EditorBrowsableState.Never), ClientInternalUseOnly]
        internal bool IsDeleted { get; private set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal GraphGroup(
            string origin,
            string originId,
            SubjectDescriptor descriptor,
            IdentityDescriptor legacyDescriptor,
            string displayName,
            ReferenceLinks links,
            string url,
            string domain,
            string principalName,
            string mailAddress,
            string description,
            string specialType,
            Guid scopeId,
            string scopeType,
            string scopeName,
            Guid localScopeId,
            Guid securingHostId,
            bool isRestrictedVisible,
            bool isCrossProject,
            bool isGlobalScope,
            bool isDeleted)
            : base(origin, originId, descriptor, legacyDescriptor, displayName, links, url, domain, principalName, mailAddress)
        {
            Description = description;
            SpecialType = specialType;
            ScopeId = scopeId;
            ScopeType = scopeType;
            ScopeName = scopeName;
            LocalScopeId = localScopeId;
            SecuringHostId = securingHostId;
            IsRestrictedVisible = isRestrictedVisible;
            IsCrossProject = isCrossProject;
            IsGlobalScope = isGlobalScope;
            IsDeleted = isDeleted;
        }

        // this is how we replace/overwrite parameters and create a new object
        // and keep our internal objects immutable
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal GraphGroup(
            GraphGroup group,
            string origin = null,
            string originId = null,
            SubjectDescriptor? descriptor = null,
            IdentityDescriptor legacyDescriptor = null,
            string displayName = null,
            ReferenceLinks links = null,
            string url = null,
            string domain = null,
            string principalName = null,
            string mailAddress = null,
            string description = null,
            string specialType = null,
            Guid? scopeId = null,
            string scopeType = null,
            string scopeName = null,
            Guid? localScopeId = null,
            Guid? securingHostId = null,
            bool? isRestrictedVisible = null,
            bool? isCrossProject = null,
            bool? isGlobalScope = null,
            bool? isDeleted = null)
            : this(origin ?? group?.Origin,
                   originId ?? group?.OriginId,
                   descriptor ?? group?.Descriptor ?? default(SubjectDescriptor),
                   legacyDescriptor ?? group?.LegacyDescriptor ?? default(IdentityDescriptor),
                   displayName ?? group?.DisplayName,
                   links ?? group?.Links,
                   url ?? group?.Url,
                   domain ?? group?.Domain,
                   principalName ?? group?.PrincipalName,
                   mailAddress ?? group?.MailAddress,
                   description ?? group?.Description,
                   specialType ?? group?.SpecialType,
                   scopeId ?? group?.ScopeId ?? default(Guid),
                   scopeType ?? group?.ScopeType,
                   scopeName ?? group?.ScopeName,
                   localScopeId ?? group?.LocalScopeId ?? default(Guid),
                   securingHostId ?? group?.SecuringHostId ?? default(Guid),
                   isRestrictedVisible ?? group?.IsRestrictedVisible ?? default(bool),
                   isCrossProject ?? group?.IsCrossProject ?? default(bool),
                   isGlobalScope ?? group?.IsGlobalScope ?? default(bool),
                   isDeleted ?? group?.IsDeleted ?? default(bool))
        { }

        // only for serialization
        protected GraphGroup() { }
    }
}
