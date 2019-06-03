using System.Runtime.Serialization;

namespace GitHub.Services.Commerce
{
    /// <summary>
    /// Represents a purchase request for requesting purchase by a user who does not have authorization to purchase.
    /// </summary>
    [DataContract]
    public class PurchaseRequest
    {
        /// <summary>
        /// Name of the offer meter
        /// </summary>
        [DataMember]
        public string OfferMeterName { get; set; }

        /// <summary>
        /// Quantity for purchase
        /// </summary>
        [DataMember]
        public int Quantity { get; set; }

        /// <summary>
        /// Reason for the purchase request
        /// </summary>
        [DataMember]
        public string Reason { get; set; }

        /// <summary>
        /// Response for this purchase request by the approver
        /// </summary>
        [DataMember]
        public PurchaseRequestResponse Response { get; set; }
    }
}
