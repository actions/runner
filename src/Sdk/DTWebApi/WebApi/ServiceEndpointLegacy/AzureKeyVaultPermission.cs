using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
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
