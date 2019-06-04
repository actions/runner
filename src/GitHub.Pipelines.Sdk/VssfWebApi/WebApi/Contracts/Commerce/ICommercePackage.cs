using System.Collections.Generic;

namespace GitHub.Services.Commerce
{
    /// <summary>
    /// Encapsulates the state of offer meter definitions and purchases
    /// </summary>
    public interface ICommercePackage
    {
        IEnumerable<OfferMeter> OfferMeters { get; set; }
        IEnumerable<OfferSubscription> OfferSubscriptions { get; set; }

        IDictionary<string, string> Configuration { get; set; }
    }
}
