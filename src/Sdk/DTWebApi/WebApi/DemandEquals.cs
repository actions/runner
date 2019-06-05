using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public sealed class DemandEquals : Demand
    {
        public DemandEquals(
            String name,
            String value)
            : base(name, value)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(value, "value");
        }

        public override Demand Clone()
        {
            return new DemandEquals(this.Name, this.Value);
        }

        protected override String GetExpression()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0} -equals {1}", this.Name, this.Value);
        }

        public override Boolean IsSatisfied(IDictionary<String, String> capabilities)
        {
            String value;
            return capabilities.TryGetValue(this.Name, out value) && this.Value.Equals(value, StringComparison.OrdinalIgnoreCase);
        }
    }
}
