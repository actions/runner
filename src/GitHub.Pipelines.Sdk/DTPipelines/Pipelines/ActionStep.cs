using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Tokens;
using Newtonsoft.Json;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ActionStep : JobStep
    {
        [JsonConstructor]
        public ActionStep()
        {
        }

        private ActionStep(ActionStep actionToClone)
            : base(actionToClone)
        {
            this.Reference = actionToClone.Reference?.Clone();

            Environment = actionToClone.Environment?.Clone();
            Inputs = actionToClone.Inputs?.Clone();
        }

        public override StepType Type => StepType.Action;

        [DataMember]
        public ActionStepDefinitionReference Reference
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken Environment { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken Inputs { get; set; }

        public override Step Clone()
        {
            return new ActionStep(this);
        }
    }
}
