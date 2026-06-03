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
    /// Nested data for background step control, matching the run-service JSON shape.
    /// </summary>
    public class BackgroundStepControlData
    {
        [JsonProperty("controlType")]
        public string ControlType { get; set; }

        [JsonProperty("stepIds")]
        public string[] StepIds { get; set; }

        [JsonProperty("parallelGroupId")]
        public string ParallelGroupId { get; set; }
    }

    /// <summary>
    /// Represents a unified background step control-flow step (wait, wait-all, cancel).
    /// Replaces the separate WaitStep, WaitAllStep, and CancelStep types.
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
            this.Data = stepToClone.Data != null ? new BackgroundStepControlData
            {
                ControlType = stepToClone.Data.ControlType,
                StepIds = stepToClone.Data.StepIds != null
                    ? (string[])stepToClone.Data.StepIds.Clone()
                    : null,
                ParallelGroupId = stepToClone.Data.ParallelGroupId,
            } : null;
            this.DisplayNameToken = stepToClone.DisplayNameToken?.Clone();
        }

        public override StepType Type => StepType.BackgroundStepControl;

        /// <summary>
        /// Nested control data, deserialized from the "backgroundStepControl" JSON property.
        /// </summary>
        [JsonProperty("backgroundStepControl")]
        public BackgroundStepControlData Data { get; set; }

        /// <summary>
        /// Convenience accessors that delegate to Data.
        /// </summary>
        [JsonIgnore]
        public string ControlType => Data?.ControlType;

        [JsonIgnore]
        public string[] StepIds => Data?.StepIds;

        [JsonIgnore]
        public string ParallelGroupId => Data?.ParallelGroupId;

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken DisplayNameToken { get; set; }

        public override Step Clone()
        {
            return new BackgroundStepControl(this);
        }
    }
}
