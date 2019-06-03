using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class AzureKeyVaultVariableGroupProviderData : VariableGroupProviderData
    {
        [DataMember(EmitDefaultValue = true)]
        public Guid ServiceEndpointId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Vault
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DateTime LastRefreshedOn
        {
            get;
            set;
        }
    }
}
