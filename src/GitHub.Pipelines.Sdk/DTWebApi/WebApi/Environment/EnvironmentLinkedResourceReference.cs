using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// EnvironmentLinkedResourceReference.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public class EnvironmentLinkedResourceReference
    {
        /// <summary>
        /// Id of the resource.
        /// </summary>
        [DataMember]
        public String Id
        {
            get;
            set;
        }
        
        /// <summary>
        /// Type of resource.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String TypeName
        {
            get;
            set;
        }
    }
}
