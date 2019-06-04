// Copyright (c) Microsoft Corporation.  All rights reserved.
using System;
using System.Diagnostics;

namespace GitHub.Services.Commerce
{
    /// <summary>
    /// Information about a resource associated with a subscription.
    /// </summary>
    [DebuggerDisplay("{OfferMeter?.Name} | {IncludedQuantity} | {CommittedQuantity}")]
    public class OfferSubscription : IOfferSubscription
    {
        /// <summary>
        /// Gets or sets the name of this resource.
        /// </summary>
        public OfferMeter OfferMeter { get; set; }

        /// <summary>
        /// Gets the renewal group.
        /// </summary>
        public ResourceRenewalGroup RenewalGroup { get; set; }

        /// <summary>
        /// Quantity commited by the user, when resources is commitment based.
        /// </summary>
        public Int32 CommittedQuantity { get; set; }

        /// <summary>
        /// Uri pointing to user action on a disabled resource. 
        /// It is based on <see cref="DisabledReason" /> value.
        /// </summary>
        public Uri DisabledResourceActionLink { get; set; }

        /// <summary>
        /// A enumeration value indicating why the resource was disabled.
        /// </summary>
        public ResourceStatusReason DisabledReason { get; set; }

        /// <summary>
        /// Quantity included for free.
        /// </summary>
        public Int32 IncludedQuantity { get; set; }

        /// <summary>
        /// Returns true if resource is can be used otherwise returns false. 
        /// <see cref="DisabledReason" /> can be used to identify why resource is disabled.
        /// </summary>
        public Boolean IsUseable { get; set; }

        /// <summary>
        /// Returns true if paid billing is enabled on the resource.
        /// Returns false for non-azure subscriptions, disabled azure subscriptions or explicitly disabled by user
        /// </summary>
        public Boolean IsPaidBillingEnabled { get; set; }

        /// <summary>
        /// Returns an integer representing the maximum quantity that can be billed for this resource.
        /// Any usage submitted over this number is automatically excluded from being sent to azure.
        /// </summary>
        public Int32 MaximumQuantity { get; set; }

        /// <summary>
        /// Returns a Date of UTC kind indicating when the next reset of quantities is going to happen.
        /// On this day at UTC 2:00 AM is when the reset will occur.
        /// </summary>
        public DateTime ResetDate { get; set; }

        /// <summary>
        /// The azure subscription id
        /// </summary>
        public Guid AzureSubscriptionId { get; set; }

        /// <summary>
        /// The unique identifier of this offer subscription
        /// </summary>
        public Guid OfferSubscriptionId { get; set; }

        /// <summary>
        /// The azure subscription name
        /// </summary>
        public string AzureSubscriptionName { get; set; }

        /// <summary>
        /// The azure subscription state
        /// </summary>
        public SubscriptionStatus AzureSubscriptionState { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is trial or preview.
        /// </summary>
        public bool IsTrialOrPreview { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is in preview.
        /// </summary>
        public bool IsPreview { get; set; }

        /// <summary>
        /// Gets the value indicating whether the puchase is canceled.
        /// </summary>
        public bool IsPurchaseCanceled { get; set; }

        /// <summary>
        /// Gets the value indicating whether current meter was purchased while the meter is still in trial
        /// </summary>
        public bool IsPurchasedDuringTrial { get; set; }

        /// <summary>
        /// Gets or sets the trial expiry date.
        /// </summary>
        public DateTime? TrialExpiryDate { get; set; }

        /// <summary>
        /// Gets or sets the start date for this resource. First install date in any state.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Indicates whether users get auto assigned this license type duing first access.
        /// </summary>
        public bool AutoAssignOnAccess { get; set; }
    }
}
