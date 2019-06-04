using System;
using System.Diagnostics.CodeAnalysis;

namespace GitHub.Services.Commerce
{
    /// <summary>
    /// Class that represents common set of properties for a raw usage event reported by TFS services.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class UsageEvent
    {
        /// <summary>
        /// Meter Id.
        /// </summary>
        public string MeterName { get; set; }

        /// <summary>
        /// Unique event identifier
        /// </summary>
        public String EventId { get; set; }

        /// <summary>
        /// Account name associated with the usage event
        /// </summary>
        public String AccountName { get; set; }

        /// <summary>
        /// User GUID associated with the usage event
        /// </summary>
        public Guid AssociatedUser { get; set; }

        /// <summary>
        /// Quantity of the usage event
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Timestamp when this billing event is billable
        /// </summary>
        public DateTime BillableDate { get; set; }

        /// <summary>
        /// Recieving Timestamp of the billing event by metering service
        /// </summary>
        public DateTime EventTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the event unique identifier.
        /// </summary>
        public Guid EventUniqueId { get; set; }

        /// <summary>
        /// Service context GUID associated with the usage event
        /// </summary>
        public Guid ServiceIdentity { get; set; }

        /// <summary>
        /// Gets or sets the billing mode for the resource involved in the usage
        /// </summary>
        public ResourceBillingMode ResourceBillingMode { get; set; }

        /// <summary>
        /// Gets or sets subscription guid of the associated account of the event
        /// </summary>
        public Guid SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets subscription anniversary day of the subscription
        /// </summary>
        public int SubscriptionAnniversaryDay { get; set; }

        /// <summary>
        /// Partition id of the account
        /// </summary>
        public int PartitionId { get; set; }

        /// <summary>
        /// Gets or sets account id of the event. Note: This is for backward compat with BI.
        /// </summary>
        public Guid AccountId { get; set; }
    }
}
