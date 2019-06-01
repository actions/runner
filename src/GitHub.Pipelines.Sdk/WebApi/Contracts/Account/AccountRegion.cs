using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Account
{
    /// <summary>
    /// Account region metadata
    /// </summary>
    [DataContract]
    public sealed class AccountRegion
    {
        /// <summary>
        /// Azure location name
        /// </summary>
        [DataMember]
        public string LocationName { get; set; }

        /// <summary>
        /// Display name of the account region
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// Whether the region is default or not
        /// </summary>
        [DataMember]
        public bool IsDefault { get; set; }
    }
}
