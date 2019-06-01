using System.Runtime.Serialization;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Validation;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [DataContract]
    internal enum DeploymentRollingOption
    {
        [EnumMember]
        Absolute,

        [EnumMember]
        Percentage
    }

    [DataContract]
    internal class DeploymentExecutionOptions
    {
        public DeploymentExecutionOptions()
        {
        }

        private DeploymentExecutionOptions(DeploymentExecutionOptions optionsToCopy)
        {
            this.RollingOption = optionsToCopy.RollingOption;
            this.RollingValue = optionsToCopy.RollingValue;
        }

        [DataMember]
        public DeploymentRollingOption RollingOption
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public uint RollingValue
        {
            get;
            set;
        }

        public DeploymentExecutionOptions Clone()
        {
            return new DeploymentExecutionOptions(this);
        }

        public void Validate(
            IPipelineContext context,
            ValidationResult result)
        {
            switch (RollingOption)
            {
                case DeploymentRollingOption.Absolute:
                    if (RollingValue == 0)
                    {
                        result.Errors.Add(new PipelineValidationError(PipelineStrings.InvalidAbsoluteRollingValue()));
                    }
                    break;

                case DeploymentRollingOption.Percentage:
                    if (RollingValue == 0 || RollingValue > 100)
                    {
                        result.Errors.Add(new PipelineValidationError(PipelineStrings.InvalidPercentageRollingValue()));
                    }
                    break;

                default:
                    result.Errors.Add(new PipelineValidationError(PipelineStrings.InvalidRollingOption(RollingOption)));
                    break;
            }
        }
    }
}
