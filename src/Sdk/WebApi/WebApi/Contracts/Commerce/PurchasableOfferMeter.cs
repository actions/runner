using System;
using System.Collections.Generic;

namespace GitHub.Services.Commerce
{
    /// <summary>
    /// The Purchasable offer meter.
    /// </summary>
    public class PurchasableOfferMeter
    {
        /// <summary>
        /// Gets or sets the offer meter definition.
        /// </summary>
        public OfferMeter OfferMeterDefinition { get; set; }

        /// <summary>
        /// Gets or sets the meter pricing (GraduatedPrice)
        /// </summary>
        public IEnumerable<KeyValuePair<double, double>> MeterPricing { get; set; }

        /// <summary>
        /// Gets or sets the estimated renewal date.
        /// </summary>
        public DateTime EstimatedRenewalDate { get; set; }

        /// <summary>
        /// Currecny code for meter pricing 
        /// </summary>
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Locale for azure subscription
        /// </summary>
        public string LocaleCode { get; set; }


    }
}
