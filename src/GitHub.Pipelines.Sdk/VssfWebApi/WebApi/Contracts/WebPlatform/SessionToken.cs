using System;
using System.Runtime.Serialization;

namespace GitHub.Services.WebPlatform
{
    [DataContract]
    public class WebSessionToken
    {
        [DataMember(Order = 1)]
        public Guid? AppId { get; set; }

        [DataMember(Order = 10)]
        public String Token { get; set; }

        [DataMember(Order = 20, EmitDefaultValue = false)]
        public String Name { get; set; }

        [DataMember(Order = 30, EmitDefaultValue = false)]
        public Boolean Force { get; set; }

        [DataMember(Order = 40, EmitDefaultValue = false)]
        public DelegatedAppTokenType? TokenType { get; set; }

        [DataMember(Order = 50, EmitDefaultValue = false)]
        public DateTime? ValidTo { get; set; }

        [DataMember(Order = 60, EmitDefaultValue = false)]
        public String NamedTokenId { get; set; }

        [DataMember(Order = 70, EmitDefaultValue = false, IsRequired = false)]
        public String PublisherName { get; set; }

        [DataMember(Order = 80, EmitDefaultValue = false, IsRequired = false)]
        public String ExtensionName { get; set; }
    }

    public enum DelegatedAppTokenType
    {
        Session,
        App
    }
}
