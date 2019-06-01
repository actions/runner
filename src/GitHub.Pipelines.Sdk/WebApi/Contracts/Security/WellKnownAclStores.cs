using System;

namespace Microsoft.VisualStudio.Services.Security
{
    /// <summary>
    /// Contains identifiers for well-known ACL (access control list) stores
    /// in the security service.
    /// </summary>
    public static class WellKnownAclStores
    {
        /// <summary>
        /// The user store is the ACL (access control list) store which is
        /// user-visible and user-editable, and which is used to evaluate effective permissions
        /// for most identities.
        /// </summary>
        public static readonly Guid User = new Guid(c_userString);

        /// <summary>
        /// The system store is the ACL (access control list) store which is
        /// not user-visible or user-editable, and is used to evaluate effective permissions on
        /// special types of identities such as licenses.
        /// </summary>
        public static readonly Guid System = new Guid(c_systemString);

        private const String c_userString = "CA3D400B-E690-47AC-83EA-FDAE80FC8D76";
        private const String c_systemString = "0C458ADA-F17C-4F66-B644-3663707D17DD";
    }
}
