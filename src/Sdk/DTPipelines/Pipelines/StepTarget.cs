using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace GitHub.DistributedTask.Pipelines
{
    public class StepTarget
    {
        [JsonConstructor]
        public StepTarget() {

        }
        [DataMember(EmitDefaultValue = false)]
        public string Target { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string Commands { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public TaskVariableRestrictions SettableVariables { get; set; }
    }
}