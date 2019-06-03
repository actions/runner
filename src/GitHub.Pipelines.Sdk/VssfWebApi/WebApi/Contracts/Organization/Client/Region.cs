using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Organization.Client
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

        /// <summary>
        /// The identifier of the service instance that supports host creations in this region
        /// </summary>
        [DataMember]
        public Guid ServiceInstanceId { get; set; }

        /// <summary>
        /// The number of hosts that are readily available for host creation in this region on this service instance
        /// </summary>
        [DataMember]
        public int AvailableHostsCount { get; set; }

        /// <summary>
        /// Whether the region is internal or not
        /// </summary>
        [DataMember]
        [Obsolete("This property is obsolete and will be removed in M148. Use RegionStatus instead.", false)]
        public bool IsInternal { get; set; }

        /// <summary>
        /// The region status
        /// </summary>
        [DataMember]
        public RegionStatus RegionStatus { get; set; }
    }

    [DataContract]
    public enum RegionStatus : byte
    {
        [EnumMember]
        Disabled = 0,

        [EnumMember]
        Internal = 1,

        [EnumMember]
        Public = 2,

        [EnumMember]
        Preflight = 3,
    }
}
