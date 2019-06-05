using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using GitHub.Services.Identity;

namespace GitHub.Services.Security
{
    [Serializable]
    [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
    [ExceptionMapping("0.0", "3.0", "SecurityException", "GitHub.Services.Security.SecurityException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public abstract class SecurityException : VssServiceException
    {
        public SecurityException(String message)
            : base(message)
        {
        }

        public SecurityException(String message, Exception ex)
            : base(message, ex)
        {
        }
    }

    /// <summary>
    /// An exception which is thrown when a permission check fails in the security service.
    /// </summary>
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "AccessCheckException", "GitHub.Framework.Server.AccessCheckException, GitHub.Framework.Server, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccessCheckException : SecurityException
    {
        /// <summary>
        /// Constructs an AccessCheckException.
        /// </summary>
        /// <param name="descriptor">The identity descriptor which was checked.</param>
        /// <param name="identityDisplayName">The display name of the identity which was checked.</param>
        /// <param name="token">The token which was checked.</param>
        /// <param name="requestedPermissions">The requested permissions, which were not satisifed by the check.</param>
        /// <param name="namespaceId">The security namespace which was checked.</param>
        /// <param name="message">A descriptive message for the exception.</param>
        public AccessCheckException(
            IdentityDescriptor descriptor,
            String identityDisplayName,
            String token,
            int requestedPermissions,
            Guid namespaceId,
            String message)
            : this(descriptor, token, requestedPermissions, namespaceId, message)
        {
            this.IdentityDisplayName = identityDisplayName;
        }

        /// <summary>
        /// Constructs an AccessCheckException.
        /// </summary>
        /// <param name="descriptor">The identity descriptor which was checked.</param>
        /// <param name="token">The token which was checked.</param>
        /// <param name="requestedPermissions">The requested permissions, which were not satisifed by the check.</param>
        /// <param name="namespaceId">The security namespace which was checked.</param>
        /// <param name="message">A descriptive message for the exception.</param>
        public AccessCheckException(
            IdentityDescriptor descriptor,
            String token,
            int requestedPermissions,
            Guid namespaceId,
            String message)
            : base(message)
        {
            ArgumentUtility.CheckForNull(descriptor, nameof(descriptor));
            ArgumentUtility.CheckForNull(token, nameof(token));
            ArgumentUtility.CheckForNull(message, nameof(message));

            this.Descriptor = descriptor;
            this.Token = token;
            this.RequestedPermissions = requestedPermissions;
            this.NamespaceId = namespaceId;
        }

        public AccessCheckException(String message)
            : base(message)
        {
        }

        /// <summary>
        /// The identity descriptor which was checked.
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public IdentityDescriptor Descriptor { get; private set; }

        /// <summary>
        /// The display name of the identity which was checked.
        /// This property may be null.
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public String IdentityDisplayName { get; private set; }

        /// <summary>
        /// The token which was checked.
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public String Token { get; private set; }

        /// <summary>
        /// The permissions which were demanded.
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public int RequestedPermissions { get; private set; }

        /// <summary>
        /// The identifier of the security namespace which was checked.
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public Guid NamespaceId { get; private set; }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidAclStoreException", "GitHub.Services.Security.InvalidAclStoreException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidAclStoreException : SecurityException
    {
        public InvalidAclStoreException(Guid namespaceId, Guid aclStoreId)
            : this(SecurityResources.InvalidAclStoreException(namespaceId, aclStoreId))
        {
        }

        public InvalidAclStoreException(String message)
            : base(message)
        {
        }

        public InvalidAclStoreException(String message, Exception ex)
            : base(message, ex)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class InvalidPermissionsException : SecurityException
    {
        public InvalidPermissionsException(Guid namespaceId, Int32 bitmask)
            : this(SecurityResources.InvalidPermissionsException(namespaceId, bitmask))
        {
        }

        public InvalidPermissionsException(String message)
            : base(message)
        {
        }

        public InvalidPermissionsException(String message, Exception ex)
            : base(message, ex)
        {
        }
    }
}
