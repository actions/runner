using System.Runtime.Serialization;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Information about an environment parsed from YML with evaluated name, URL and Deployment will be evaluated on runner
    /// </summary>
    [DataContract]
    public class ActionsEnvironmentReference
    {
        public ActionsEnvironmentReference(string name)
        {
            Name = name;
        }

        [DataMember(EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken Url { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken Deployment { get; set; }
    }
}
