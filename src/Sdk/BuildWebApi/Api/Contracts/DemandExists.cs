using System;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents an "exists" demand.
    /// </summary>
    /// <remarks>
    /// This demand is satisfied as long as the named capability exists, regardless of its value.
    /// </remarks>
    public sealed class DemandExists : Demand
    {
        public DemandExists(
            String name)
            : this(name, null)
        {
        }

        public DemandExists(
            String name,
            ISecuredObject securedObject)
            : base(name, null, securedObject)
        {
        }

        /// <summary>
        /// Clones this object.
        /// </summary>
        /// <returns></returns>
        public override Demand Clone()
        {
            return new DemandExists(this.Name);
        }

        protected override String GetExpression()
        {
            return this.Name;
        }
    }
}
