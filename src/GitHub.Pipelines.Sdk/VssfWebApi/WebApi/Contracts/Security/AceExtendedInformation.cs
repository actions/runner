using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Security
{
    /// <summary>
    /// Holds the inherited and effective permission information for a given AccessControlEntry.
    /// </summary>
    [DataContract]
    public sealed class AceExtendedInformation
    {
        public AceExtendedInformation()
        { 
        }

        /// <summary>
        /// Creates a new AceExtendedInformation object with the specified information.
        /// </summary>
        /// <param name="inheritedAllow">The allow bits received from inheritance.</param>
        /// <param name="inheritedDeny">The deny bits received from inheritance.</param>
        /// <param name="effectiveAllow">The effective allow bits.</param>
        /// <param name="effectiveDeny">The effective deny bits.</param>
        public AceExtendedInformation(
            Int32 inheritedAllow,
            Int32 inheritedDeny,
            Int32 effectiveAllow,
            Int32 effectiveDeny)
        {
            InheritedAllow = inheritedAllow;
            InheritedDeny = inheritedDeny;
            EffectiveAllow = effectiveAllow;
            EffectiveDeny = effectiveDeny;
        }

        /// <summary>
        /// These are the permissions that are inherited for this
        /// identity on this token.  If the token does not inherit 
        /// permissions this will be 0.  Note that any permissions that
        /// have been explicitly set on this token for this identity, or 
        /// any groups that this identity is a part of, are not included here.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Int32 InheritedAllow { get; set; }

        /// <summary>
        /// These are the permissions that are inherited for this
        /// identity on this token.  If the token does not inherit 
        /// permissions this will be 0.  Note that any permissions that
        /// have been explicitly set on this token for this identity, or 
        /// any groups that this identity is a part of, are not included here.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Int32 InheritedDeny { get; set; }

        /// <summary>
        /// This is the combination of all of the explicit and inherited
        /// permissions for this identity on this token.  These are the
        /// permissions used when determining if a given user has permission
        /// to perform an action.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Int32 EffectiveAllow { get; set; }

        /// <summary>
        /// This is the combination of all of the explicit and inherited
        /// permissions for this identity on this token.  These are the
        /// permissions used when determining if a given user has permission
        /// to perform an action.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Int32 EffectiveDeny { get; set; }
    }
}
