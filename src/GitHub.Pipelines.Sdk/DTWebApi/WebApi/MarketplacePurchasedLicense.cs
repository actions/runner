using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Represents a purchase of resource units in a secondary marketplace.
    /// </summary>
    /// <remarks>
    /// The type of resource purchased (pipelines, minutes) is not represented here.
    /// </remarks>
    [DataContract]
    public sealed class MarketplacePurchasedLicense
    {
        /// <summary>
        /// The Marketplace display name.
        /// </summary>
        /// <example>"GitHub"</example>
        [DataMember(EmitDefaultValue = false)]
        public String MarketplaceName { get; set; }

        /// <summary>
        /// The name of the identity making the purchase as seen by the marketplace
        /// </summary>
        /// <example>"AppPreview, Microsoft, etc."</example>
        [DataMember(EmitDefaultValue = false)]
        public String PurchaserName { get; set; }

        /// <summary>
        /// The quantity purchased.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 PurchaseUnitCount { get; set; }
    }
}
