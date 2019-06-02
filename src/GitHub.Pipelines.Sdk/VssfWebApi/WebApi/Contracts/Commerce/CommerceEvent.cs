using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Commerce
{
    public class CommerceEvent : ICommerceEvent
    {
        public string EventId { get; set; }
    
        public DateTime EventTime { get; set; }

        public string EventName { get; set; }

        public Guid OrganizationId { get; set; }

        public string OrganizationName { get; set; }

        public Guid CollectionId { get; set; }

        public string CollectionName { get; set; }

        public Guid SubscriptionId { get; set; }

        public string MeterName { get; set; }

        public string GalleryId { get; set; }

        /// <summary>
        /// Quantity for current billing cycle
        /// </summary>
        public int CommittedQuantity { get; set; }

        /// <summary>
        /// Quantity for next billing cycle
        /// </summary>
        public int CurrentQuantity { get; set; }

        /// <summary>
        /// Previous quantity in case of upgrade/downgrade
        /// </summary>
        public int PreviousQuantity { get; set; }

        /// <summary>
        /// Billed quantity (prorated) passed to Azure commerce
        /// </summary>
        public double BilledQuantity { get; set; }

        /// <summary>
        /// Quantity available for free
        /// </summary>
        public int IncludedQuantity { get; set; }

        public int? PreviousIncludedQuantity { get; set; }

        public int MaxQuantity { get; set; }

        public int? PreviousMaxQuantity { get; set; }

        public string RenewalGroup { get; set; }

        public string EventSource { get; set; }

        /// <summary>
        /// Onpremise or hosted
        /// </summary>
        public string Environment { get; set; }

        public Guid UserIdentity { get; set; }

        public Guid ServiceIdentity { get; set; }

        public DateTime? TrialStartDate { get; set; }

        public DateTime? TrialEndDate { get; set; }

        public DateTime? EffectiveDate { get; set; }

        public string Version { get; set; }
    }
}
