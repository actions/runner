// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TaskGroupQueryOrder.cs" company="Microsoft Corporation">
//   2012-2023, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Specifies the desired ordering of taskGroups. 
    /// </summary>
    [DataContract]
    public enum TaskGroupQueryOrder
    {
        /// <summary>
        /// Order by createdon ascending.
        /// </summary>
        [EnumMember]
        CreatedOnAscending = 0,

        /// <summary>
        /// Order by createdon descending.
        /// </summary>
        [EnumMember]
        CreatedOnDescending = 1,

    }
}