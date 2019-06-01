using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public sealed class DemandExists : Demand
    {
        public DemandExists(String name)
            : base(name, null)
        {
        }

        public override Demand Clone()
        {
            return new DemandExists(this.Name);
        }

        protected override String GetExpression()
        {
            return this.Name;
        }

        public override Boolean IsSatisfied(IDictionary<String, String> capabilities)
        {
            return capabilities.ContainsKey(this.Name);
        }

        public new void Update(String value)
        {
            // Exists can not override value
            throw new NotImplementedException();
        }
    }
}
