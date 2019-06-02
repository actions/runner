using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    public enum PlanTemplateType
    {
        [DataMember]
        None = 0,

        [DataMember]
        Designer = 1,

        [DataMember]
        System = 2,

        [DataMember]
        Yaml = 3,
    }
}
