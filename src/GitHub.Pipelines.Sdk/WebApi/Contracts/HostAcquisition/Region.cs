using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.HostAcquisition
{
    [DataContract]
    public sealed class Region
    {
        /// <summary>
        /// Name identifier for the region.
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Display name for the region.
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// Short name used in Microsoft Azure. Ex: southcentralus, westcentralus, southindia, etc.
        /// </summary>
        [DataMember]
        public string NameInAzure { get; set; }

        /// <summary>
        /// Whether the region is default or not
        /// </summary>
        [DataMember]
        public bool IsDefault { get; set; }
    }
}
