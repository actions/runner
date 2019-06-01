using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Commerce
{
    public class UsageEventAggregate : IUsageEventAggregate
    {
        /// <summary>
        /// Gets or sets start time of the aggregated value, inclusive
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets end time of the aggregated value, exclusive
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets resource that the aggregated value represents
        /// </summary>
        public ResourceName Resource { get; set; }

        /// <summary>
        /// Gets or sets quantity of the resource used from start time to end time
        /// </summary>
        public Int32 Value { get; set; }
    }
}
