using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class InputValidationRequest
    {
        public IDictionary<String, ValidationItem> Inputs
        {
            get
            {
                if (m_inputs == null)
                {
                    m_inputs = new Dictionary<String, ValidationItem>(StringComparer.Ordinal);
                }

                return m_inputs;
            }
        }

        [DataMember(Name = "Inputs")]
        private Dictionary<String, ValidationItem> m_inputs;
    }
}
