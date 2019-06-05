namespace GitHub.DistributedTask.WebApi
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public class HelpLink
    {
        [DataMember]
        public String Text { get; set; }

        [DataMember]
        public Uri Url { get; set; }
    }
}
