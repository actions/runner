// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PublishTaskGroupMetadata.cs" company="Microsoft Corporation">
//   2012-2023, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace GitHub.DistributedTask.WebApi
{
    public class PublishTaskGroupMetadata
    {
        public Guid TaskGroupId { get; set; }
        // This is revision of task group that is getting published
        public int TaskGroupRevision { get; set; }
        public int ParentDefinitionRevision { get; set; }
        public Boolean Preview { get; set; }
        public String Comment { get; set; }
    }
}
