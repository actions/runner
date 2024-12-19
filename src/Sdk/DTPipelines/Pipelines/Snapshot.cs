using System;
using System.Runtime.Serialization;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    public class Snapshot
    {
        public Snapshot(string imageName, string condition = null, string version = null)
        {
            ImageName = imageName;
            Condition = condition ?? $"{PipelineTemplateConstants.Success}()";
            Version = version ?? "1.*";
        }

        [DataMember(EmitDefaultValue = false)]
        public String ImageName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public String Condition { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public String Version { get; set; }
    }
}
