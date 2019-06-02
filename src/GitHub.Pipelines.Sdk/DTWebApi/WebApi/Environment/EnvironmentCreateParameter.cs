using System;
using System.ComponentModel;
using System.Runtime.Serialization;


namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    /// <summary>
    /// Properties to create Environment.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public class EnvironmentCreateParameter
    {
        /// <summary>
        /// Name of the environment.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// Description of the environment.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Description
        {
            get;
            set;
        }
    }
}
