using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class CounterVariable
    {
        public CounterVariable(String prefix, Int32 seed, Int32 value)
        {
            m_prefix = prefix;
            m_seed = seed;
            m_value = value;
        }

        public String Prefix => m_prefix;

        public Int32 Seed => m_seed;

        public Int32 Value => m_value;

        [DataMember(Name = "prefix", EmitDefaultValue = false)]
        private readonly String m_prefix;
        [DataMember(Name = "seed", EmitDefaultValue = false)]
        private readonly Int32 m_seed;
        [DataMember(Name = "value", EmitDefaultValue = false)]
        private readonly Int32 m_value;
    }
}
