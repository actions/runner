using GitHub.Services.Common;
using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Profile
{
    public class AttributesQueryContext : ICloneable
    {
        public AttributesQueryContext(
            AttributesScope scope,
            DateTimeOffset? modifiedSince = null,
            int? modifiedAfterRevision = null,
            CoreProfileAttributes? coreAttributes = null,
            string containerName = null)
        {
            if (scope.HasFlag(~(AttributesScope.Application | AttributesScope.Core))
                || (!scope.HasFlag(AttributesScope.Application) && !scope.HasFlag(AttributesScope.Core)))
            {
                throw new ArgumentException(string.Format("The scope '{0}' is not supported for this operation.", scope));
            }

            Scope = scope;
            ModifiedSince = modifiedSince;
            ModifiedAfterRevision = modifiedAfterRevision;

            if (scope.HasFlag(AttributesScope.Application))
            {
                ProfileArgumentValidation.ValidateApplicationContainerName(containerName);
                ContainerName = containerName;
            }
            else
            {
                ContainerName = null;
            }

            if (scope.HasFlag(AttributesScope.Core))
            {
                CoreAttributes = coreAttributes ?? CoreProfileAttributes.All;
            }
            else
            {
                CoreAttributes = null;
            }
        }

        public AttributesQueryContext(AttributesScope scope, string containerName)
            : this(scope, null, null, CoreProfileAttributes.All, containerName)
        {
        }

        /// <remarks>
        /// Deprecated constructor. The operation to 'get attributes since a certain point in time' is now deprecated.
        /// </remarks>>
        public AttributesQueryContext(AttributesScope scope, DateTimeOffset modifiedSince, string containerName = null)
            : this(scope, modifiedSince, null, CoreProfileAttributes.All, containerName)
        {
        }

        public AttributesQueryContext(AttributesScope scope, int modifiedAfterRevision, string containerName = null)
            : this(scope, null, modifiedAfterRevision, CoreProfileAttributes.All, containerName)
        {
        }

        [DataMember(IsRequired = true)]
        public AttributesScope Scope { get; private set; }

        [DataMember]
        public string ContainerName { get; private set; }

        [DataMember]
        public DateTimeOffset? ModifiedSince { get; private set; }

        [DataMember]
        public int? ModifiedAfterRevision { get; private set; }

        [DataMember]
        public CoreProfileAttributes? CoreAttributes { get; private set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = obj as AttributesQueryContext;

            return this.Equals(other);
        }

        public bool Equals(AttributesQueryContext other)
        {
            return (Scope == other.Scope &&
                    VssStringComparer.AttributesDescriptor.Equals(ContainerName, other.ContainerName) &&
                    ModifiedSince == other.ModifiedSince &&
                    ModifiedAfterRevision == other.ModifiedAfterRevision);
        }

        public override int GetHashCode()
        {
            int hashCode = Scope.GetHashCode();
            hashCode = (hashCode * 499) ^ (ContainerName != null ? ContainerName.ToLowerInvariant().GetHashCode() : 0);
            hashCode = (hashCode * 499) ^ (ModifiedSince != null ? ModifiedSince.GetHashCode() : 0);
            hashCode = (hashCode * 499) ^ (ModifiedAfterRevision != null ? ModifiedAfterRevision.GetHashCode() : 0);
            hashCode = (hashCode * 499) ^ (CoreAttributes != null ? CoreAttributes.GetHashCode() : 0);

            return hashCode;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    /// <summary>
    /// Used to specify the scope of a set of attributes.
    /// </summary>
    /// <remarks>
    /// A profile attribute is either a core attribute or an attribute beloging to some application container.
    /// A core attribute belongs to scope AttributesScope.Core.
    /// An attribute stored under some application container belongs to scope AttributesScope.Application.
    /// An attribute always belongs to scope AttributesScope.Core | AttributesScope.Application.
    /// </remarks>
    [DataContract, Flags]
    public enum AttributesScope
    {
        [EnumMember]
        Core = 0x1,

        [EnumMember]
        Application = 0x2,
    }

}
