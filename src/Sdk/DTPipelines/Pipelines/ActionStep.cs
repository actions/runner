using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.Pipelines
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
            ContextName = actionToClone?.ContextName;
            DisplayNameToken = actionToClone.DisplayNameToken?.Clone();
        }

        public override StepType Type => StepType.Action;

        [DataMember]
        public ActionStepDefinitionReference Reference
        {
            get;
            set;
        }

        // TODO: After TFS and legacy phases/steps/ect are removed, lets replace the DisplayName in the base class with this value and remove this additional prop
        [DataMember(EmitDefaultValue = false)]
        public TemplateToken DisplayNameToken { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public String ContextName { get; set; }

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
