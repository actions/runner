using System;
using System.ComponentModel;
using System.Runtime.Serialization;


namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public class EnvironmentReference
    {
        [DataMember]
        public Int32 Id { get; set; }

        [DataMember]
        public String Name { get; set; }
    }
}
