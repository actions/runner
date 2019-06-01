using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.WebApi
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