using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// This is useful in getting a list of deployment targets, filtered by the result of their last job.
    /// </summary>
    [Flags]
    [DataContract]
    public enum TaskAgentJobResultFilter
    {
        /// <summary>
        /// Only those deployment targets on which last job failed (**Abandoned**, **Canceled**, **Failed**, **Skipped**).
        /// </summary>
        [EnumMember]
        Failed = 1,

        /// <summary>
        /// Only those deployment targets on which last job Passed (**Succeeded**, **Succeeded with issues**).
        /// </summary>
        [EnumMember]
        Passed = 2,

        /// <summary>
        /// Only those deployment targets that never executed a job.
        /// </summary>
        [EnumMember]
        NeverDeployed = 4,

        /// <summary>
        /// All deployment targets.
        /// </summary>
        [EnumMember]
        All = Failed | Passed | NeverDeployed
    }
}
