using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Services.Commerce
{
    public interface ICommerceEvent
    {
        string EventId { get; set; }

        DateTime EventTime { get; set; }

        string EventName { get; set; }

        Guid OrganizationId { get; set; }

        string OrganizationName { get; set; }

        Guid CollectionId { get; set; }

        string CollectionName { get; set; }

        Guid SubscriptionId { get; set; }

        string MeterName { get; set; }

        string GalleryId { get; set; }

        /// <summary>
        /// Quantity for current billing cycle
        /// </summary>
        int CommittedQuantity { get; set; }

        /// <summary>
        /// Quantity for next billing cycle
        /// </summary>
        int CurrentQuantity { get; set; }

        /// <summary>
        /// Previous quantity in case of upgrade/downgrade
        /// </summary>
        int PreviousQuantity { get; set; }

        /// <summary>
        /// Billed quantity (prorated) passed to Azure commerce
        /// </summary>
        double BilledQuantity { get; set; }

        // quantity available for free
        int IncludedQuantity { get; set; }

        int? PreviousIncludedQuantity { get; set; }

        int MaxQuantity { get; set; }

        int? PreviousMaxQuantity { get; set; }

        string RenewalGroup { get; set; }

        string EventSource { get; set; }

        /// <summary>
        /// Onpremise or hosted
        /// </summary>
        string Environment { get; set; }

        Guid UserIdentity { get; set; }

        Guid ServiceIdentity { get; set; }

        DateTime? TrialStartDate { get; set; }

        DateTime? TrialEndDate { get; set; }

        DateTime? EffectiveDate { get; set; }

        string Version { get; set; }
    }
}
