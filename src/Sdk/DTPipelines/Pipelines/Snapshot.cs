using System;
using System.Runtime.Serialization;

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
    }
}
