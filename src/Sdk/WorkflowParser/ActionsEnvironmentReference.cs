#nullable enable

using System.Runtime.Serialization;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;

namespace GitHub.Actions.WorkflowParser
{
    /// <summary>
    /// Information about an environment parsed from YML with evaluated name, URL will be evaluated on runner
    /// </summary>
    [DataContract]
    public sealed class ActionsEnvironmentReference
    {
        public ActionsEnvironmentReference(string name)
        {
            Name = name;
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public TemplateToken? Url { get; set; }
    }
}
