using System.Collections.Generic;

namespace GitHub.Services.Commerce
{
    public class CommercePackage : ICommercePackage
    {
        public IEnumerable<OfferMeter> OfferMeters { get; set; }
        public IEnumerable<OfferSubscription> OfferSubscriptions { get; set; }

        public IDictionary<string, string> Configuration { get; set; }
    }
}
