using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.Services.Commerce
{
    /// <summary>
    /// Put call content is deserialized to this object.
    /// </summary>
    public class CsmSubscriptionRequest
    {
        /// <summary>
        /// Identifies the quota id in the properties.
        /// </summary>
        private const string QuotaIdKey = "quotaId";

        /// <summary>
        /// Free tier quota identifier identifying that a subscription is from free tier.
        /// This array could include more quota id's in future to identify new free tier subscriptions.
        /// </summary>
        private static readonly string[] FreeTierQuotaIdentifiers = { "DreamSpark_2015-02-01" };

        /// <summary>
        /// Gets or sets the subscription state.
        /// </summary>
        /// <value>
        /// The state.
        /// </value>
        public CsmSubscriptionState State { get; set; }
        
        /// <summary>
        /// Gets or sets the subscription registration date.
        /// </summary>
        /// <value>
        /// The registration date.
        /// </value>
        public DateTime RegistrationDate { get; set; }

        /// <summary>
        /// Gets or sets the subscription identifier.
        /// </summary>
        /// <value>
        /// The subscription identifier.
        /// </value>
        public Guid SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets a collection of extended properties for the request.
        /// </summary>
        public Dictionary<String, object> Properties { get; set; }

        /// <summary>
        /// Gets or sets the quota identifier for the subscription
        /// </summary>
        /// <value>
        /// The quota identifier of the offer from azure billing. Ex: DreamSpark_2015-02-01.
        /// </value>
        public string QuotaId { get; private set; }

        /// <summary>
        /// Identifies the source of the subscription
        /// </summary>
        /// <value>
        /// One of the enumeration values indicating whether subscription is Enterprise Agreement, Internal, Free Tier or Normal.
        /// </value>
        public SubscriptionSource Source { get; set; }

        /// <summary>
        /// Adjust received data.
        /// </summary>
        /// <returns></returns>
        public bool AdjustData()
        {
            if (this.Properties?.ContainsKey(QuotaIdKey) == true)
            {
                this.QuotaId = this.Properties["quotaId"]?.ToString().Trim() ?? string.Empty;
            }

            if (!string.IsNullOrEmpty(this.QuotaId) &&
                FreeTierQuotaIdentifiers.Contains(this.QuotaId, StringComparer.OrdinalIgnoreCase) &&
                this.State == CsmSubscriptionState.Registered)
            {
                this.Source = SubscriptionSource.FreeTier;
            }

            return true;
        }

        /// <summary>
        /// Check whether receieved status is valid.
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            if (this.State == CsmSubscriptionState.Unknown)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// This is done so that we can trace the values passed.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"Subscription State:{this.State}; Registration Date:{this.RegistrationDate}: QuotaId: {this.QuotaId}";
        }
    }
}