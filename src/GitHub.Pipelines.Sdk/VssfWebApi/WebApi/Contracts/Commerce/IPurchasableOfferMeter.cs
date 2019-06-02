using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Commerce
{
    /// <summary>
    /// The Purchasable offer meter.
    /// </summary>
    public interface IPurchasableOfferMeter
    {
        /// <summary>
        /// Gets or sets the offer meter definition.
        /// </summary>
        OfferMeter OfferMeterDefinition { get; set; }

        /// <summary>
        /// Gets or sets the meter pricing (GraduatedPrice)
        /// </summary>
        IEnumerable<KeyValuePair<double, double>> MeterPricing { get; set; }

        /// <summary>
        /// Gets or sets the estimated renewal date.
        /// </summary>
        DateTime EstimatedRenewalDate { get; set; }
    }
}