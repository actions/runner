// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectReference.cs" company="Microsoft Corporation">
//   2012-2023, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class ProjectReference
    {
        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public string Name { get; set; }
    }
}
