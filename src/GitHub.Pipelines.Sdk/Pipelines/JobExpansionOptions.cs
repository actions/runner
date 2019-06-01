using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    public class JobExpansionOptions
    {
        public JobExpansionOptions(ICollection<String> configurations)
        {
            AddConfigurations(configurations);
        }

        internal JobExpansionOptions(IDictionary<String, Int32> configurations)
        {
            UpdateConfigurations(configurations);
        }

        internal JobExpansionOptions(
            String configuration,
            Int32 attemptNumber = NoSpecifiedAttemptNumber)
        {
            if (!String.IsNullOrEmpty(configuration))
            {
                this.Configurations.Add(configuration, attemptNumber);
            }
        }

        /// <summary>
        /// Specifies a filter for the expansion of specific Phase configurations.
        /// The key is the configuration name, the value is the explicitly requested 
        ///   attempt number. 
        /// If mapping is null, there is no filter and all configurations will be
        ///   produced.
        /// </summary>
        internal IDictionary<String, Int32> Configurations
        {
            get
            {
                if (m_configurations == null)
                {
                    m_configurations = new Dictionary<String, Int32>(StringComparer.OrdinalIgnoreCase);
                }
                return m_configurations;
            }
        }

        public Boolean IsIncluded(String configuration)
        {
            return m_configurations == null || m_configurations.ContainsKey(configuration);
        }

        /// <summary>
        /// Add new configurations, with no specified custom attempt number
        /// </summary>
        public void AddConfigurations(ICollection<String> configurations)
        {
            if (configurations == null)
            {
                return;
            }

            var localConfigs = this.Configurations;
            foreach (var c in configurations)
            {
                if (!localConfigs.ContainsKey(c))
                {
                    localConfigs[c] = NoSpecifiedAttemptNumber;
                }
            }
        }

        /// <summary>
        /// add (or replace) any configurations and their associated attempt numbers with new provided values. 
        /// </summary>
        public void UpdateConfigurations(IDictionary<String, Int32> configurations)
        {
            if (configurations == null)
            {
                return;
            }

            var localConfigs = this.Configurations;
            foreach (var pair in configurations)
            {
                localConfigs[pair.Key] = pair.Value;
            }
        }

        /// <summary>
        /// returns custom attempt number or JobExpansionOptions.NoSpecifiedAttemptNumber if none specified.
        /// </summary>
        /// <param name="configuration">configuration or "job name"</param>
        public Int32 GetAttemptNumber(String configuration)
        {
            if (m_configurations != null && m_configurations.TryGetValue(configuration, out Int32 number))
            {
                return number;
            }

            return NoSpecifiedAttemptNumber;
        }

        public const Int32 NoSpecifiedAttemptNumber = -1;
        private Dictionary<String, Int32> m_configurations;
    }
}
