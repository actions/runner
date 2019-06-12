// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VariableGroupQueryOrder.cs" company="Microsoft Corporation">
//   2012-2023, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace GitHub.DistributedTask.WebApi
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Specifies the desired ordering of variableGroups.
    /// </summary>
    [DataContract]
    public enum  VariableGroupQueryOrder
    {
        /// <summary>
        /// Order by id ascending.
        /// </summary>
        [EnumMember]
        IdAscending = 0,

        /// <summary>
        /// Order by id descending.
        /// </summary>
        [EnumMember]
        IdDescending = 1,
    }
}
