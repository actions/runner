using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public enum TaskAgentPoolMaintenanceScheduleDays
    {
        /// <summary>
        /// Do not run.
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// Run on Monday.
        /// </summary>
        [EnumMember]
        Monday = 1,

        /// <summary>
        /// Run on Tuesday.
        /// </summary>
        [EnumMember]
        Tuesday = 2,

        /// <summary>
        /// Run on Wednesday.
        /// </summary>
        [EnumMember]
        Wednesday = 4,

        /// <summary>
        /// Run on Thursday.
        /// </summary>
        [EnumMember]
        Thursday = 8,

        /// <summary>
        /// Run on Friday.
        /// </summary>
        [EnumMember]
        Friday = 16,

        /// <summary>
        /// Run on Saturday.
        /// </summary>
        [EnumMember]
        Saturday = 32,

        /// <summary>
        /// Run on Sunday.
        /// </summary>
        [EnumMember]
        Sunday = 64,

        /// <summary>
        /// Run on all days of the week.
        /// </summary>
        [EnumMember]
        All = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday,
    }
}
