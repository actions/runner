using System.Runtime.Serialization;

namespace GitHub.Services.Licensing
{
    [DataContract]
    public class AccountRights
    {
        public AccountRights()
            : this(VisualStudioOnlineServiceLevel.None, string.Empty)
        {
        }

        public AccountRights(VisualStudioOnlineServiceLevel level)
            : this(level, string.Empty)
        {
        }

        public AccountRights(VisualStudioOnlineServiceLevel level, string reason)
        {
            this.Level = level;
            this.Reason = reason;
        }

        [DataMember(IsRequired = true)]
        public VisualStudioOnlineServiceLevel Level { get; private set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string Reason { get; private set; }
    }
}
