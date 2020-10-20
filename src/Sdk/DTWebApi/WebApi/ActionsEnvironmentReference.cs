using System.Runtime.Serialization;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Information about an environment parsed from YML with evaluated name, URL will be evaluated on runner
    /// </summary>
    [DataContract]
    public class ActionsEnvironmentReference
    {
        public ActionsEnvironmentReference(string name)
        {
            Name = name;
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public TemplateToken Url { get; set; }
    }
}
