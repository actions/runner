using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class ActionDownloadInfo
    {
        [DataMember(EmitDefaultValue = false)]
        public ActionDownloadAuthentication Authentication { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string NameWithOwner { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string ResolvedNameWithOwner { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string ResolvedSha { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string TarballUrl { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Ref { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string ZipballUrl { get; set; }
    }

    [DataContract]
    public class ActionDownloadAuthentication
    {
        [DataMember(EmitDefaultValue = false)]
        public DateTime ExpiresAt { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Token { get; set; }
    }
}
