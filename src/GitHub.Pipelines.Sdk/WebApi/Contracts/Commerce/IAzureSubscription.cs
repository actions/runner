using System;

namespace Microsoft.VisualStudio.Services.Commerce
{
    public interface IAzureSubscription
    {
        Guid Id { get; set; }
        SubscriptionStatus Status { get; set; }
        AccountProviderNamespace Namespace { get; set; }
        AzureOfferType? OfferType { get; set; }
        SubscriptionSource Source { get; set; }
        int AnniversaryDay { get; set; }
        DateTime Created { get; set; }
        DateTime LastUpdated { get; set; }
    }
}