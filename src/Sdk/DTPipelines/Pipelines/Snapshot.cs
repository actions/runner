using System;
using System.Runtime.Serialization;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    public class Snapshot
    {
        public Snapshot(string imageName)
        {
            ImageName = imageName;
        }

        [DataMember(EmitDefaultValue = false)]
        public String ImageName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public String Condition
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Version  { get; set; }
    }
}
