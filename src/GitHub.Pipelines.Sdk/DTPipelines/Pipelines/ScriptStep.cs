using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Tokens;
using Newtonsoft.Json;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [DataContract]
    [Obsolete("Deprecated", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ScriptStep : JobStep
    {
        [JsonConstructor]
        public ScriptStep()
        {
        }

        private ScriptStep(ScriptStep copy)
            : base(copy)
        {
            Environment = copy.Environment?.Clone();
            Inputs = copy.Inputs?.Clone();
        }

        public override StepType Type => StepType.Script;

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken Environment { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken Inputs { get; set; }

        public override Step Clone()
        {
            return new ScriptStep(this);
        }
    }
}
