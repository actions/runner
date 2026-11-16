using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.Pipelines
{
    /// <summary>
    /// Known control-flow types for background step control steps.
    /// Wire values must match run-service constants (wait, wait-all, cancel).
    /// </summary>
    public static class BackgroundControlTypes
    {
        public const string Wait = "wait";
        public const string WaitAll = "wait-all";
        public const string Cancel = "cancel";
    }

    /// <summary>
    /// Represents a unified background step control-flow step (wait, wait-all, cancel).
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class BackgroundStepControl : JobStep
    {
        [JsonConstructor]
        public BackgroundStepControl()
        {
        }

        private BackgroundStepControl(BackgroundStepControl stepToClone)
            : base(stepToClone)
        {
            this.ControlType = stepToClone.ControlType;
            this.StepIds = stepToClone.StepIds != null
                ? (string[])stepToClone.StepIds.Clone()
                : null;
            this.DisplayNameToken = stepToClone.DisplayNameToken?.Clone();
        }

        public override StepType Type => StepType.BackgroundStepControl;

        [DataMember(EmitDefaultValue = false)]
        public string ControlType { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string[] StepIds { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken DisplayNameToken { get; set; }

        public override Step Clone()
        {
            return new BackgroundStepControl(this);
        }
    }
}
