#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Actions.WorkflowParser
{
    [DataContract]
    public sealed class Strategy
    {
        public Strategy()
        {
            FailFast = true;
        }

        [DataMember(Name = "failFast", EmitDefaultValue = true)]
        public Boolean FailFast { get; set; }

        [DataMember(Name = "maxParallel", EmitDefaultValue = false)]
        public int MaxParallel { get; set; }

        [IgnoreDataMember]
        public List<StrategyConfiguration> Configurations
        {
            get
            {
                if (m_configuration is null)
                {
                    m_configuration = new List<StrategyConfiguration>();
                }
                return m_configuration;
            }
        }

        [DataMember(Name = "configuration", EmitDefaultValue = false)]
        private List<StrategyConfiguration>? m_configuration;
    }
}
