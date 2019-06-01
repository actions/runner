using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Security
{
    /// <summary>
    /// Represents a set of evaluated permissions.
    /// </summary>
    [DataContract]
    public class PermissionEvaluationBatch : ISecuredObject
    {
        /// <summary>
        /// Array of permission evaluations to evaluate.
        /// </summary>
        [DataMember(IsRequired = true)]
        public PermissionEvaluation[] Evaluations { get; set; }

        /// <summary>
        /// True if members of the Administrators group should always pass the security check.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool AlwaysAllowAdministrators { get; set; }

        public Guid NamespaceId => SecuritySecurityConstants.NamespaceId;
        public int RequiredPermissions => SecuritySecurityConstants.Read;
        public String GetToken() => SecuritySecurityConstants.RootToken;
    }

    /// <summary>
    /// Represents an evaluated permission.
    /// </summary>
    [DataContract]
    public class PermissionEvaluation : ISecuredObject
    {
        /// <summary>
        /// Security namespace identifier for this evaluated permission.
        /// </summary>
        [DataMember(IsRequired = true)]
        public Guid SecurityNamespaceId { get; set; }

        /// <summary>
        /// Security namespace-specific token for this evaluated permission.
        /// </summary>
        [DataMember(IsRequired = true)]
        public string Token { get; set; }

        /// <summary>
        /// Permission bit for this evaluated permission.
        /// </summary>
        [DataMember]
        public int Permissions { get; set; }

        /// <summary>
        /// Permission evaluation value.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public bool Value { get; set; }

        public Guid NamespaceId => SecuritySecurityConstants.NamespaceId;
        public int RequiredPermissions => SecuritySecurityConstants.Read;
        public String GetToken() => SecuritySecurityConstants.RootToken;
    }
}
