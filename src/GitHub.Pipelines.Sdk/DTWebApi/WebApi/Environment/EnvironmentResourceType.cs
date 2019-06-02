using System;
using System.ComponentModel;


namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    /// <summary>
    /// EnvironmentResourceType.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Flags]
    public enum EnvironmentResourceType
    {
        Undefined = 0,

        /// <summary>
        /// Unknown resource type
        /// </summary>
        Generic = 1,

        /// <summary>
        /// Virtual machine resource type
        /// </summary>
        VirtualMachine = 2,

        /// <summary>
        /// Kubernetes resource type
        /// </summary>
        Kubernetes = 4
    }
}