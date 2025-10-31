#nullable enable

using System.Runtime.Serialization;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;

namespace GitHub.Actions.WorkflowParser
{
    [DataContract]
    public class Snapshot
    {
        [DataMember(EmitDefaultValue = false)]
        public required string ImageName { get; set; }
        
        [DataMember(EmitDefaultValue = false)]
        public BasicExpressionToken? If
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public required string Version  { get; set; }
    }
}
