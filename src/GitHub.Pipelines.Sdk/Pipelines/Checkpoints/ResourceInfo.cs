using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi.Internal;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Checkpoints
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    [ClientIgnore]
    public class ResourceInfo
    {
        [DataMember(EmitDefaultValue = false)]
        public String Id { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public String Name { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public String TypeName { get; set; }
    }
}
