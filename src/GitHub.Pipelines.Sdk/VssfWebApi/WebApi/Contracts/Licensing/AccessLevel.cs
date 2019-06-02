using Microsoft.VisualStudio.Services.Account;
using Microsoft.VisualStudio.Services.Licensing;
using System.Runtime.Serialization;

namespace Microsoft.Azure.DevOps.Licensing.WebApi
{
    /// <summary>
    /// License assigned to a user
    /// </summary>
    [DataContract]
    public class AccessLevel
    {
        /// <summary>
        /// Licensing Source (e.g. Account. MSDN etc.)
        /// </summary>
        [DataMember]
        public LicensingSource LicensingSource { get; set; }

        /// <summary>
        /// Type of Account License (e.g. Express, Stakeholder etc.)
        /// </summary>
        [DataMember]
        public AccountLicenseType AccountLicenseType { get; set; }

        /// <summary>
        /// Type of MSDN License (e.g. Visual Studio Professional, Visual Studio Enterprise etc.)
        /// </summary>
        [DataMember]
        public MsdnLicenseType MsdnLicenseType { get; set; }

        /// <summary>
        /// Display name of the License
        /// </summary>
        [DataMember]
        public string LicenseDisplayName { get; set; }

        /// <summary>
        /// User status in the account
        /// </summary>
        [DataMember]
        public AccountUserStatus Status { get; set; }

        /// <summary>
        /// Status message.
        /// </summary>
        [DataMember]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Assignment Source of the License (e.g. Group, Unknown etc.
        /// </summary>
        [DataMember]
        public AssignmentSource AssignmentSource { get; set; }
    }
}
