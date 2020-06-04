using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class ActionReference
    {
        [DataMember]
        public string NameWithOwner
        {
            get;
            set;
        }

        [DataMember]
        public string Ref
        {
            get;
            set;
        }
    }
}
