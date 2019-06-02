// Copyright (c) Microsoft Corporation.  All rights reserved.
using System;

namespace Microsoft.VisualStudio.Services.Commerce
{
    /// <summary>
    /// Information about a resource associated with a subscription.
    /// </summary>
    public interface IOfferSubscription
    {
        /// <summary>
        /// Gets the name of this resource.
        /// </summary>
        OfferMeter OfferMeter { get; }

        /// <summary>
        /// Gets the renewal group.
        /// </summary>
        ResourceRenewalGroup RenewalGroup { get; }

        /// <summary>
        /// Quantity commited by the user, when resources is commitment based.
        /// </summary>
        Int32 CommittedQuantity { get; }

        /// <summary>
        /// Uri pointing to user action on a disabled resource.
        /// It is based on <see cref="DisabledReason" /> value.
        /// </summary>
        Uri DisabledResourceActionLink { get; }

        /// <summary>
        /// A enumeration value indicating why the resource was disabled.
        /// </summary>
        ResourceStatusReason DisabledReason { get; }

        /// <summary>
        /// Quantity included for free.
        /// </summary>
        Int32 IncludedQuantity { get; }

        /// <summary>
        /// Returns true if resource is can be used otherwise returns false. 
        /// <see cref="DisabledReason" /> can be used to identify why resource is disabled.
        /// </summary>
        Boolean IsUseable { get; }

        /// <summary>
        /// Returns true if paid billing is enabled on the resource.
        /// Returns false for non-azure subscriptions, disabled azure subscriptions or explicitly disabled by user
        /// </summary>
        Boolean IsPaidBillingEnabled { get; }

        /// <summary>
        /// Returns an integer representing the maximum quantity that can be billed for this resource.
        /// Any usage submitted over this number is automatically excluded from being sent to azure.
        /// </summary>
        Int32 MaximumQuantity { get; }

        /// <summary>
        /// Returns a Date of UTC kind indicating when the next reset of quantities is going to happen.
        /// On this day at UTC 2:00 AM is when the reset will occur.
        /// </summary>
        DateTime ResetDate { get; }

        /// <summary>
        /// The azure subscription id
        /// </summary>
        Guid AzureSubscriptionId { get; set; }

        /// <summary>
        /// The azure subscription name
        /// </summary>
        string AzureSubscriptionName { get; set; }

        /// <summary>
        /// The azure subscription state
        /// </summary>
        SubscriptionStatus AzureSubscriptionState { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is trial or preview.
        /// </summary>
        bool IsTrialOrPreview { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is in preview.
        /// </summary>
        bool IsPreview { get; set; }

        /// <summary>
        /// Gets the value indicating whether the puchase is canceled.
        /// </summary>
        bool IsPurchaseCanceled { get; set; }

        /// <summary>
        /// Gets the value indicating whether current meter was purchased while the meter is still in trial
        /// </summary>
        bool IsPurchasedDuringTrial { get; set; }

        /// <summary>
        /// Gets or sets the trial expiry date.
        /// </summary>
        DateTime? TrialExpiryDate { get; set; }

        /// <summary>
        /// Gets or sets the start date for this resource. First install date in any state.
        /// </summary>
        DateTime? StartDate { get; set; }

        /// <summary>
        /// Indicates whether users get auto assigned this license type duing first access.
        /// </summary>
        bool AutoAssignOnAccess { get; set; }
    }
}
