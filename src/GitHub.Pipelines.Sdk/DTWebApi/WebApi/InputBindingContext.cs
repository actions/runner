using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class InputBindingContext
    {
        /// <summary>
        /// Value of the input
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Value
        {
            get;
            set;
        }
    }
}
