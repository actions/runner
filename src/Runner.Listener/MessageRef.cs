using System.Runtime.Serialization;

namespace GitHub.Runner.Listener
{
    [DataContract]
    public sealed class MessageRef
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
    }
}