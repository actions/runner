using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class AzureRoleAssignmentPermission : AzurePermission
    {

        [DataMember]
        public Guid RoleAssignmentId { get; set; }

        public AzureRoleAssignmentPermission() : base(AzurePermissionResourceProviders.AzureRoleAssignmentPermission)
        {
        }
    }
}
