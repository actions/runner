// Copyright (c) Microsoft Corporation.  All rights reserved.
using System;

namespace Microsoft.VisualStudio.Services.Commerce
{
    /// <summary>
    /// Information about a resource associated with a subscription.
    /// </summary>
    public interface ISubscriptionResource
    {
        /// <summary>
        /// Gets the name of this resource.
        /// </summary>
        ResourceName Name { get; }

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
    }
}
