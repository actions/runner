using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class ActionDownloadInfoCollection
    {
        [DataMember]
        public IDictionary<string, ActionDownloadInfo> Actions
        {
            get;
            set;
        }
    }
}
