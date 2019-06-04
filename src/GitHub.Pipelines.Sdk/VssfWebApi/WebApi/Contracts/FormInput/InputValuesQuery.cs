using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Services.FormInput
{
    [DataContract]
    public class InputValuesQuery
    {
        /// <summary>
        /// Subscription containing information about the publisher/consumer and the current input values
        /// </summary>
        [DataMember]
        public Object Resource { get; set; }

        /// <summary>
        /// The input values to return on input, and the result from the consumer on output.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public IList<InputValues> InputValues { get; set; }

        [DataMember]
        public IDictionary<String, String> CurrentValues { get; set; }
    }
}
