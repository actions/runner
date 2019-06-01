using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class AzureKeyVaultPermission : AzureResourcePermission
    {
        [DataMember]
        public String Vault { get; set; }

        public AzureKeyVaultPermission() : base(AzurePermissionResourceProviders.AzureKeyVaultPermission)
        {
        }
    }
}
