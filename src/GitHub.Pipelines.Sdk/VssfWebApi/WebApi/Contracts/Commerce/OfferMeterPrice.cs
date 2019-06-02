using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Commerce
{
    public class OfferMeterPrice : IOfferMeterPrice
    {
        /// <summary>
        /// The meter Name which identifies the offer meter this plan is associated with
        /// </summary>
        public string MeterName { get; set; }

        /// <summary>
        /// The Name of the plan, which is usually in the format "{publisher}:{offer}:{plan}"
        /// </summary>
        public string PlanName { get; set; }

        /// <summary>
        /// Region price is for
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Currency code
        /// </summary>
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Plan Quantity 
        /// </summary>
        public double Quantity { get; set; }

        /// <summary>
        /// Plan Price
        /// </summary>
        public double Price { get; set; }

        public override bool Equals(Object obj)
        {
            if (obj == null || !(obj is OfferMeterPrice))
            {
                return false;
            }
            OfferMeterPrice instance = (OfferMeterPrice)obj;
            if (string.Equals(this.MeterName, instance.MeterName) &&
                string.Equals(this.PlanName, instance.PlanName) &&
                string.Equals(this.Region, instance.Region) &&
                string.Equals(this.CurrencyCode, instance.CurrencyCode) &&
                this.Quantity == instance.Quantity &&
                this.Price == instance.Price)
            {
                return true;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return this.MeterName.GetHashCode() + this.PlanName.GetHashCode() + this.Region.GetHashCode();
        }
    }
}
