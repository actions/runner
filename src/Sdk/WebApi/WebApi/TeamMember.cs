using System.Runtime.Serialization;

namespace GitHub.Services.WebApi
{
    [DataContract]
    public class TeamMember
    {
        [DataMember(Name = "isTeamAdmin", EmitDefaultValue = false)]
        public bool IsTeamAdmin { get; set; }

        [DataMember(Name = "identity", EmitDefaultValue = false)]
        public IdentityRef Identity { get; set; }
    }
}
