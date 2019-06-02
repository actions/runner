using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class AzureKeyVaultVariableValue: VariableValue
    {
        public AzureKeyVaultVariableValue()
        {
        }

        public AzureKeyVaultVariableValue(AzureKeyVaultVariableValue value)
            : this(value.Value, value.IsSecret, value.Enabled, value.ContentType, value.Expires)
        {
        }

        public AzureKeyVaultVariableValue(String value, Boolean isSecret, Boolean enabled, String contentType, DateTime? expires)
            :base(value, isSecret)
        {
            Enabled = enabled;
            ContentType = contentType;
            Expires = expires;
        }

        [DataMember(EmitDefaultValue = true)]
        public Boolean Enabled
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = true)]
        public String ContentType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DateTime? Expires
        {
            get;
            set;
        }
    }
}
