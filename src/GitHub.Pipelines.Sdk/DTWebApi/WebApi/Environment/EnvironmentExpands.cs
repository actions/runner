using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Properties to be included or expanded in environment objects. This is useful when getting a single environment.
    /// </summary>
    [Flags]
    [DataContract]
    public enum EnvironmentExpands
    {
        /// <summary>
        /// No additional properties
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// Include resource references referring to the environment.
        /// </summary>
        [EnumMember]
        ResourceReferences = 1
    }
}
