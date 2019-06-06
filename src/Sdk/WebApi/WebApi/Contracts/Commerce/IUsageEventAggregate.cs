using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Services.Commerce
{
    /// <summary>
    /// Represents the aggregated usage of a resource over a time span
    /// </summary>
    public interface IUsageEventAggregate
    {
        /// <summary>
        /// Gets or sets start time of the aggregated value, inclusive
        /// </summary>
        DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets end time of the aggregated value, exclusive
        /// </summary>
        DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets resource that the aggregated value represents
        /// </summary>
        ResourceName Resource { get; set; }

        /// <summary>
        /// Gets or sets quantity of the resource used from start time to end time
        /// </summary>
        Int32 Value { get; set; }
    }
}
