using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines
{
    public class TaskVariableRestrictions
    {
        public TaskVariableRestrictions() {
            Allowed = new List<string>();
        }

        [DataMember(EmitDefaultValue = false)]
        public IList<string> Allowed { get; }
    }
}