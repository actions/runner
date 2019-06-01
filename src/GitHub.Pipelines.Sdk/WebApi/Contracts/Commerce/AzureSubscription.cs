using System;

namespace Microsoft.VisualStudio.Services.Commerce
{
    public class AzureSubscription : IAzureSubscription
    {
        public Guid Id { get; set; }
        public SubscriptionStatus Status { get; set; }
        public AccountProviderNamespace Namespace { get; set; }
        public AzureOfferType? OfferType { get; set; }
        public SubscriptionSource Source { get; set; }
        public int AnniversaryDay { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}