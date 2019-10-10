using System;
using System.Globalization;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents an "equals" demand.
    /// </summary>
    /// <remarks>
    /// This demand is satisfied when the value of the named capability matches the value stored in the demand.
    /// </remarks>
    public sealed class DemandEquals : Demand
    {
        public DemandEquals(
            String name,
            String value)
            : this(name, value, null)
        {
        }

        public DemandEquals(
            String name,
            String value,
            ISecuredObject securedObject)
            : base(name, value, securedObject)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(value, "value");
        }

        /// <summary>
        /// Clones this object.
        /// </summary>
        /// <returns></returns>
        public override Demand Clone()
        {
            return new DemandEquals(this.Name, this.Value);
        }

        protected override String GetExpression()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0} -equals {1}", this.Name, this.Value);
        }
    }
}
