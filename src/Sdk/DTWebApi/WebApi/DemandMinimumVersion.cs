using GitHub.DistributedTask.Pipelines;
using GitHub.Services.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public sealed class DemandMinimumVersion : Demand
    {
        public DemandMinimumVersion(
            String name,
            String value)
            : base(name, value)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(value, "value");
        }

        public override Demand Clone()
        {
            return new DemandMinimumVersion(this.Name, this.Value);
        }

        protected override String GetExpression()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0} -gtVersion {1}", this.Name, this.Value);
        }

        public override Boolean IsSatisfied(IDictionary<String, String> capabilities)
        {
            String value;
            if (capabilities.TryGetValue(this.Name, out value))
            {
                // return true if our version is less than or equal to the capability version from the agent
                return CompareVersion(this.Value, value) <= 0;
            }

            // same as capabilityVersion == null
            return false; 
        }

        public static Int32 CompareVersion(String semanticVersion1, String semanticVersion2) 
        {
            // compare == first - second (-1 means second is greater, 1 means first is greater, 0 means they are equal)
            Version version1 = ParseVersion(semanticVersion1);
            Version version2 = ParseVersion(semanticVersion2);
            if (version1 == null && version2 == null)
            {
                // they are both null, so they are equal
                return 0;
            }
            else if (version1 == null)
            {
                // version2 is greater
                return -1;
            }
            else if (version2 == null)
            {
                // version1 is greater
                return 1;
            }

            return version1.CompareTo(version2);
        }

        /// <summary>
        /// Gets the minimum agent version demand from the specified set of demands. Agent version demands are removed
        /// from the input set.
        /// </summary>
        /// <param name="demands">The demands</param>
        /// <returns>The highest minimum version required based in the input set</returns>
        public static DemandMinimumVersion MaxAndRemove(ISet<Demand> demands)
        {
            DemandMinimumVersion minAgentVersion = null;
            var demandsCopy = demands.Where(x => x.Name.Equals(PipelineConstants.AgentVersionDemandName, StringComparison.OrdinalIgnoreCase)).OfType<DemandMinimumVersion>().ToList();
            foreach (var demand in demandsCopy)
            {
                if (minAgentVersion == null || CompareVersion(demand.Value, minAgentVersion.Value) > 0)
                {
                    minAgentVersion = demand;
                }

                demands.Remove(demand);
            }

            return minAgentVersion;
        }

        public static DemandMinimumVersion Max(IEnumerable<Demand> demands)
        {
            DemandMinimumVersion minAgentVersion = null;
            foreach (var demand in demands.Where(x => x.Name.Equals(PipelineConstants.AgentVersionDemandName, StringComparison.OrdinalIgnoreCase)).OfType<DemandMinimumVersion>())
            {
                if (minAgentVersion == null || CompareVersion(demand.Value, minAgentVersion.Value) > 0)
                {
                    minAgentVersion = demand;
                }
            }

            return minAgentVersion;
        }

        public static Version ParseVersion(String versionString)
        {
            Version version = null;
            if (!String.IsNullOrEmpty(versionString))
            {
                int index = versionString.IndexOf('-');
                if (index > 0)
                {
                    versionString = versionString.Substring(0, index);
                }

                if (!Version.TryParse(versionString, out version))
                {
                    // If we couldn't parse it, set it back to null
                    version = null;
                }
            }

            return version;
        }
    }
}
