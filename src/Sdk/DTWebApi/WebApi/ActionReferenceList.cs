using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class ActionReferenceList
    {
        [DataMember]
        public IList<ActionReference> Actions
        {
            get;
            set;
        }
    }
}
