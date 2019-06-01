//-----------------------------------------------------------------------
// <copyright file="ISubscriptionAccount.cs" company="Microsoft">
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
    public interface ISubscriptionAccount
    {
        /// <summary>
        /// Gets or sets the account identifier. Usually a guid.
        /// </summary>
        /// <value>
        /// The account identifier.
        /// </value>
        Guid AccountId { get; set; }

        /// <summary>
        /// Gets or sets the account host type.
        /// </summary>
        int AccountHostType { get; set; }

        /// <summary>
        /// Gets or sets the name of the account.
        /// </summary>
        /// <value>
        /// The name of the account.
        /// </value>
        string AccountName { get; set; }

        /// <summary>
        /// Gets or sets the subscription identifier.
        /// </summary>
        /// <value>
        /// The subscription identifier.
        /// </value>
        Guid? SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the subscription status.
        /// </summary>
        /// <value>
        /// The subscription status.
        /// </value>
        SubscriptionStatus SubscriptionStatus { get; set; }

        /// <summary>
        /// Gets or sets the resource group.
        /// </summary>
        /// <value>
        /// The resource group.
        /// </value>
        string ResourceGroupName { get; set; }

        /// <summary>
        /// Gets or sets the geo location.
        /// </summary>
        /// <value>
        /// The geo location.
        /// </value>
        string GeoLocation { get; set; }

        /// <summary>
        /// Gets or sets the subscription locale
        /// </summary>
        string Locale { get; set; }

        /// <summary>
        /// Gets or sets the subscription address country display name
        /// </summary>
        string RegionDisplayName { get; set; }

        /// <summary>
        /// A dictionary of service urls, mapping the service owner to the service owner url
        /// </summary>
        /// <value>
        /// Urls which can be used to access account apis
        /// </value>
        IDictionary<Guid, Uri> ServiceUrls { get; set; }

        /// <summary>
        /// Gets or sets the account tenantId.
        /// </summary>
        /// <value>
        /// If the account is not linked to a tenant this willl be Guid.Empty
        /// </value>
        Guid AccountTenantId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the calling user identity owns or is a PCA of the account.
        /// </summary>
        /// <value>
        ///   <c>true</c> if user owns or is a PCA of the account otherwise, <c>false</c>.
        /// </value>
        bool IsAccountOwner { get; set; }

        /// <summary>
        /// Gets or sets the azure resource name.
        /// </summary>
        /// <value>
        /// The resource name representing the link between an account and subscription.
        /// </value>
        string ResourceName { get; set; }

        /// <summary>
        /// Gets or sets the azure subscription name
        /// </summary>
        /// <value>
        /// This represent display name of azure subscription
        /// </value>
        string SubscriptionName { get; set; }

        /// <summary>
        /// Gets or set the flag to enable purchase via subscription. 
        /// </summary>
        /// <value>
        /// Flag indicate if purchase can be happen against billing entity for given (gallery) item
        /// This is run time property 
        /// </value>
        bool IsEligibleForPurchase { get; set; }

        /// <summary>
        /// get or set IsPrepaidFundSubscription
        /// </summary>
        /// <value>
        /// Flag indicates if pre paid fund warning message needs to display for subscription 
        /// </value>
        bool IsPrepaidFundSubscription { get; set; }

        /// <summary>
        /// get or set IsPricingPricingAvailable
        /// </summary>
        /// <value>
        /// Flag indicates if meter pricing needs to display for subscription 
        /// </value>
        bool IsPricingAvailable { get; set; }

        /// <summary>
        /// get or set subscription offer code
        /// </summary>
        //[Obsolete("Please use the OfferType Property", error:false)]
        string SubscriptionOfferCode { get; set; }

        /// <summary>
        /// Gets or sets the Offer Type of this subscription.
        /// A value of null means, this value has not been evaluated.
        /// </summary>
        AzureOfferType? OfferType { get; set; }

        /// <summary>
        ///get or set tenant id of subscription 
        /// </summary>
        Guid? SubscriptionTenantId { get; set; }

        /// <summary>
        ///get or set object id of subscruption admin 
        /// </summary>
        Guid? SubscriptionObjectId { get; set; }

        /// <summary>
        ///get or set purchase Error Reason
        /// </summary>
        PurchaseErrorReason FailedPurchaseReason { get; set; }

    }
}
