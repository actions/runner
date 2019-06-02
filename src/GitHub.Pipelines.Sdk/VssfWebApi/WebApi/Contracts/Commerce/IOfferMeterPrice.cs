using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Commerce
{
    public interface IOfferMeterPrice
    {
        /// <summary>
        /// The meter Name which identifies the offer meter this plan is associated with
        /// </summary>
        string MeterName { get; set; }

        /// <summary>
        /// Name of the plan, which is usually in the format "{publisher}:{offer}:{plan}"
        /// </summary>
        string PlanName { get; set; }

        /// <summary>
        /// Region price is for
        /// </summary>
        string Region { get; set; }

        /// <summary>
        /// Currency code
        /// </summary>
        string CurrencyCode { get; set; }

        /// <summary>
        /// Plan Quantity 
        /// </summary>
        double Quantity { get; set; }

        /// <summary>
        /// Plan Price
        /// </summary>
        double Price { get; set; }
    }

}
