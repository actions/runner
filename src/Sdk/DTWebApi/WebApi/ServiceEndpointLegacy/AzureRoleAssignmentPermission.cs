using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
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
