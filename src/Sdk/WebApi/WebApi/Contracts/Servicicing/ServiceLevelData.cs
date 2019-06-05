using System.Runtime.Serialization;

namespace GitHub.Services.Servicing
{
    [DataContract]
    public class ServiceLevelData
    {
        [DataMember(Order = 0, EmitDefaultValue = false, IsRequired = false)]
        public string ServicingAreas { get; set; }

        [DataMember(Order = 10)]
        public string ConfigurationDatabaseServiceLevel { get; set; }

        [DataMember(Order = 20)]
        public string DeploymentHostServiceLevel { get; set; }

        [DataMember(Order = 30, EmitDefaultValue = false, IsRequired = false)]
        public string AccountDatabaseServiceLevel { get; set; }

        [DataMember(Order = 40, EmitDefaultValue = false, IsRequired = false)]
        public string AccountHostServiceLevel { get; set; }

        [DataMember(Order = 50, EmitDefaultValue = false, IsRequired = false)]
        public string CollectionDatabaseServiceLevel { get; set; }

        [DataMember(Order = 60, EmitDefaultValue = false, IsRequired = false)]
        public string CollectionHostServiceLevel { get; set; }
    }
}
