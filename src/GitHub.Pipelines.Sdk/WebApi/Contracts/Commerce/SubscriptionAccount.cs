//-----------------------------------------------------------------------
// <copyright file="SubscriptionAccount.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// The subscription account.
// </summary>
//-----------------------------------------------------------------------

namespace Microsoft.VisualStudio.Services.Commerce
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The subscription account.
    /// Add Sub Type and Owner email later.
    /// </summary>
    public class SubscriptionAccount : ISubscriptionAccount
    {
        /// <summary>
        /// Gets or sets the account identifier. Usually a guid.
        /// </summary>
        /// <value>
        /// The account identifier.
        /// </value>
        public Guid AccountId { get; set; }

        /// <summary>
        /// Gets or sets the account host type.
        /// </summary>
        public int AccountHostType { get; set; }

        /// <summary>
        /// Gets or sets the name of the account.
        /// </summary>
        /// <value>
        /// The name of the account.
        /// </value>
        public string AccountName { get; set; }

        /// <summary>
        /// Gets or sets the subscription identifier.
        /// </summary>
        /// <value>
        /// The subscription identifier.
        /// </value>
        public Guid? SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the subscription status.
        /// </summary>
        /// <value>
        /// The subscription status.
        /// </value>
        public SubscriptionStatus SubscriptionStatus { get; set; }

        /// <summary>
        /// Gets or sets the resource group.
        /// </summary>
        /// <value>
        /// The resource group.
        /// </value>
        public string ResourceGroupName { get; set; }

        /// <summary>
        /// Gets or sets the geo location.
        /// </summary>
        /// <value>
        /// The geo location.
        /// </value>
        public string GeoLocation { get; set; }

        /// <summary>
        /// Gets or sets the subscription address country code
        /// </summary>
        public string Locale { get; set; }

        /// <summary>
        /// Gets or sets the subscription address country display name
        /// </summary>
        public string RegionDisplayName { get; set; }

        /// <summary>
        /// A dictionary of service urls, mapping the service owner to the service owner url
        /// </summary>
        /// <value>
        /// Urls which can be used to access account apis
        /// </value>
        public IDictionary<Guid, Uri> ServiceUrls { get; set; }

        /// <summary>
        /// Gets or sets the account tenantId.
        /// </summary>
        /// <value>
        /// If the account is not linked to a tenant this willl be Guid.Empty
        /// </value>
        public Guid AccountTenantId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the calling user identity owns or is a PCA of the account.
        /// </summary>
        /// <value>
        ///   <c>true</c> if user owns or is a PCA of the account otherwise, <c>false</c>.
        /// </value>
        public bool IsAccountOwner { get; set; }

        /// <summary>
        /// Gets or sets the azure resource name.
        /// </summary>
        /// <value>
        /// The resource name representing the link between an account and subscription.
        /// </value>
        public string ResourceName { get; set; }

        /// <summary>
        /// Gets or sets the azure subscription name
        /// </summary>
        /// <value>
        /// This represent display name of azure subscription
        /// </value>
        public string SubscriptionName { get; set; }

        /// <summary>
        /// Gets or set the flag to enable purchase via subscription. 
        /// </summary>
        /// <value>
        /// Flag indicate if purchase can be happen against billing entity for given (gallery) item
        /// This is run time property 
        /// </value>
        public bool IsEligibleForPurchase { get; set; }

        /// <summary>
        /// get or set IsPrepaidFundSubscription
        /// </summary>
        /// <value>
        /// Flag indicates if pre paid fund warning message needs to display for subscription 
        /// </value>
        public bool IsPrepaidFundSubscription { get; set; }

        /// <summary>
        /// get or set IsPricingPricingAvailable
        /// </summary>
        /// <value>
        /// Flag indicates if meter pricing needs to display for subscription 
        /// </value>
        public bool IsPricingAvailable { get; set; }

        /// <summary>
        /// get or set subscription offer code
        /// </summary>
        //[Obsolete("Please use the OfferType Property", error:false)]
        public string SubscriptionOfferCode { get; set; }

        /// <summary>
        /// Gets or sets the Offer Type of this subscription.
        /// </summary>
        public AzureOfferType? OfferType { get; set; } = null;

        /// <summary>
        /// tenant id of subscription 
        /// </summary>
        public Guid? SubscriptionTenantId { get; set; }

        /// <summary>
        /// object id of subscription admin 
        /// </summary>
        public Guid? SubscriptionObjectId { get; set; }

        /// <summary>
        /// Purchase Error Reason
        /// </summary>
        public PurchaseErrorReason FailedPurchaseReason { get; set; }

    }
}
