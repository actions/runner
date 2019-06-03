using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace GitHub.Services.GroupLicensingRule
{
    [DataContract]
    public enum GroupLicensingRuleStatus
    {
        /// <summary>
        /// Rule is created or updated, but apply is pending
        /// </summary>
        ApplyPending = 0,

        /// <summary>
        /// Rule is applied
        /// </summary>
        Applied = 1,

        /// <summary>
        /// The group rule was incompatible
        /// </summary>
        Incompatible = 5,

        /// <summary>
        /// Rule failed to apply unexpectedly and should be retried
        /// </summary>
        UnableToApply = 10,
    }

    public static class GroupRuleStatusExtensions
    {
        /// <summary>
        /// Returns the error with the highest severity
        /// </summary>
        /// <param name="statuses"></param>
        /// <returns></returns>
        public static GroupLicensingRuleStatus HighestSeverity(this IEnumerable<GroupLicensingRuleStatus> statuses)
        {
            // coincidentally the enum is in order already, but the logic is centralized here for future flexibility
            return statuses.Max();
        }
    }
}
